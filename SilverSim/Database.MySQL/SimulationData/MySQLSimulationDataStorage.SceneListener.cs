﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using MySql.Data.MySqlClient;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Statistics;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage
    {
        readonly RwLockedList<MySQLSceneListener> m_SceneListenerThreads = new RwLockedList<MySQLSceneListener>();
        public class MySQLSceneListener : SceneListener
        {
            readonly string m_ConnectionString;
            readonly UUID m_RegionID;
            readonly RwLockedList<MySQLSceneListener> m_SceneListenerThreads;

            public MySQLSceneListener(string connectionString, UUID regionID, RwLockedList<MySQLSceneListener> sceneListenerThreads)
            {
                m_ConnectionString = connectionString;
                m_RegionID = regionID;
                m_SceneListenerThreads = sceneListenerThreads;
            }

            public UUID RegionID
            {
                get
                {
                    return m_RegionID;
                }
            }

            public QueueStat GetStats()
            {
                int count = m_StorageMainRequestQueue.Count;
                return new QueueStat(count != 0 ? "PROCESSING" : "IDLE", count, (uint)m_ProcessedPrims);
            }

            int m_ProcessedPrims;

            protected override void StorageMainThread()
            {
                try
                {
                    m_SceneListenerThreads.Add(this);
                    Thread.CurrentThread.Name = "Storage Main Thread: " + m_RegionID.ToString();
                    List<string> primDeletionRequests = new List<string>();
                    List<string> primItemDeletionRequests = new List<string>();
                    List<string> objectDeletionRequests = new List<string>();
                    List<string> updateObjectsRequests = new List<string>();
                    List<string> updatePrimsRequests = new List<string>();
                    List<string> updatePrimItemsRequests = new List<string>();

                    C5.TreeDictionary<uint, int> knownSerialNumbers = new C5.TreeDictionary<uint, int>();
                    C5.TreeDictionary<uint, int> knownInventorySerialNumbers = new C5.TreeDictionary<uint, int>();
                    C5.TreeDictionary<uint, List<UUID>> knownInventories = new C5.TreeDictionary<uint, List<UUID>>();

                    string replaceIntoObjects = string.Empty;
                    string replaceIntoPrims = string.Empty;
                    string replaceIntoPrimItems = string.Empty;

                    while (!m_StopStorageThread || m_StorageMainRequestQueue.Count != 0)
                    {
                        ObjectUpdateInfo req;
                        try
                        {
                            req = m_StorageMainRequestQueue.Dequeue(1000);
                        }
                        catch
                        {
                            continue;
                        }

                        int serialNumber = req.SerialNumber;
                        int knownSerial;
                        int knownInventorySerial;
                        bool updatePrim = false;
                        bool updateInventory = false;
                        if (req.IsKilled)
                        {
                            /* has to be processed */
                            string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                            string partID = req.Part.ID.ToString();
                            primDeletionRequests.Add(string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", sceneID, partID));
                            primItemDeletionRequests.Add(string.Format("(RegionID LIKE '{0}' AND PrimID LIKE '{1}')", sceneID, partID));
                            knownSerialNumbers.Remove(req.LocalID);
                            if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                            {
                                objectDeletionRequests.Add(string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", sceneID, partID));
                            }
                        }
                        else if (knownSerialNumbers.Contains(req.LocalID))
                        {
                            knownSerial = knownSerialNumbers[req.LocalID];
                            if (req.Part.ObjectGroup.IsAttached || req.Part.ObjectGroup.IsTemporary)
                            {
                                string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                                string partID = req.Part.ID.ToString();
                                primDeletionRequests.Add(string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", sceneID, partID));
                                primItemDeletionRequests.Add(string.Format("(RegionID LIKE '{0}' AND PrimID LIKE '{1}')", sceneID, partID));
                                if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                                {
                                    objectDeletionRequests.Add(string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", sceneID, partID));
                                }
                            }
                            else
                            {
                                if (knownSerial != serialNumber && !req.Part.ObjectGroup.IsAttached && !req.Part.ObjectGroup.IsTemporary)
                                {
                                    /* prim update */
                                    updatePrim = true;
                                    updateInventory = true;
                                }

                                if (knownInventorySerialNumbers.Contains(req.LocalID))
                                {
                                    knownInventorySerial = knownSerialNumbers[req.LocalID];
                                    /* inventory update */
                                    updateInventory = knownInventorySerial != req.Part.Inventory.InventorySerial;
                                }
                            }
                        }
                        else if (req.Part.ObjectGroup.IsAttached || req.Part.ObjectGroup.IsTemporary)
                        {
                            /* ignore it */
                            continue;
                        }
                        else
                        {
                            updatePrim = true;
                            updateInventory = true;
                        }

                        int newPrimInventorySerial = req.Part.Inventory.InventorySerial;

                        int count = Interlocked.Increment(ref m_ProcessedPrims);
                        if (count % 100 == 0)
                        {
                            m_Log.DebugFormat("Processed {0} prims", count);
                        }

                        if (updatePrim)
                        {
                            Dictionary<string, object> primData = GenerateUpdateObjectPart(req.Part);
                            ObjectGroup grp = req.Part.ObjectGroup;
                            primData.Add("RegionID", grp.Scene.ID);
                            if (replaceIntoPrims.Length == 0)
                            {
                                replaceIntoPrims = MySQLUtilities.GenerateFieldNames(primData);
                            }
                            updatePrimsRequests.Add("(" + MySQLUtilities.GenerateValues(primData) + ")");
                            knownSerialNumbers[req.LocalID] = req.SerialNumber;

                            Dictionary<string, object> objData = GenerateUpdateObjectGroup(grp);
                            if (replaceIntoObjects.Length == 0)
                            {
                                replaceIntoObjects = MySQLUtilities.GenerateFieldNames(objData);
                            }
                            updateObjectsRequests.Add("(" + MySQLUtilities.GenerateValues(objData) + ")");
                        }

                        if (updateInventory)
                        {
                            Dictionary<UUID, ObjectPartInventoryItem> items = new Dictionary<UUID, ObjectPartInventoryItem>();
                            foreach (ObjectPartInventoryItem item in req.Part.Inventory.ValuesByKey1)
                            {
                                items.Add(item.ID, item);
                            }

                            if (knownInventories.Contains(req.Part.LocalID))
                            {
                                string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                                string partID = req.Part.ID.ToString();
                                foreach (UUID itemID in knownInventories[req.Part.LocalID])
                                {
                                    if (!items.ContainsKey(itemID))
                                    {
                                        primItemDeletionRequests.Add(string.Format("(RegionID LIKE '{0}' AND PrimID LIKE '{1}' AND InventoryID LIKE '{2}')",
                                            sceneID, partID, itemID.ToString()));
                                    }
                                }

                                foreach (KeyValuePair<UUID, ObjectPartInventoryItem> kvp in items)
                                {
                                    Dictionary<string, object> data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
                                    data["RegionID"] = req.Part.ObjectGroup.Scene.ID;
                                    if (replaceIntoPrimItems.Length == 0)
                                    {
                                        replaceIntoPrimItems = MySQLUtilities.GenerateFieldNames(data);
                                    }
                                    updatePrimItemsRequests.Add("(" + MySQLUtilities.GenerateValues(data) + ")");
                                }
                            }
                            else
                            {
                                foreach (KeyValuePair<UUID, ObjectPartInventoryItem> kvp in items)
                                {
                                    Dictionary<string, object> data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
                                    data["RegionID"] = req.Part.ObjectGroup.Scene.ID;
                                    if (replaceIntoPrimItems.Length == 0)
                                    {
                                        replaceIntoPrimItems = MySQLUtilities.GenerateFieldNames(data);
                                    }
                                    updatePrimItemsRequests.Add("(" + MySQLUtilities.GenerateValues(data) + ")");
                                }
                            }
                            knownInventories[req.Part.LocalID] = new List<UUID>(items.Keys);
                            knownInventorySerialNumbers[req.Part.LocalID] = newPrimInventorySerial;
                        }

                        bool emptyQueue = m_StorageMainRequestQueue.Count == 0;
                        bool processUpdateObjects = updateObjectsRequests.Count != 0;
                        bool processUpdatePrims = updatePrimsRequests.Count != 0;
                        bool processUpdatePrimItems = updatePrimItemsRequests.Count != 0;

                        if (((emptyQueue || processUpdateObjects) && objectDeletionRequests.Count > 0) || objectDeletionRequests.Count > 256)
                        {
                            string elems = string.Join(" OR ", objectDeletionRequests);
                            try
                            {
                                string command = "DELETE FROM objects WHERE " + elems;
                                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                objectDeletionRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Object deletion failed", e);
                            }
                        }

                        if (((emptyQueue || processUpdatePrims) && primDeletionRequests.Count > 0) || primDeletionRequests.Count > 256)
                        {
                            string elems = string.Join(" OR ", primDeletionRequests);
                            try
                            {
                                string command = "DELETE FROM prims WHERE " + elems;
                                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                primDeletionRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Prim deletion failed", e);
                            }
                        }

                        if (((emptyQueue || processUpdatePrimItems) && primItemDeletionRequests.Count > 0) || primItemDeletionRequests.Count > 256)
                        {
                            string elems = string.Join(" OR ", primItemDeletionRequests);
                            try
                            {
                                string command = "DELETE FROM primitems WHERE " + elems;
                                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                primItemDeletionRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Object deletion failed", e);
                            }
                        }

                        if ((emptyQueue && updateObjectsRequests.Count > 0) || updateObjectsRequests.Count > 256)
                        {
                            string command = "REPLACE INTO objects (" + replaceIntoObjects + ") VALUES " + string.Join(",", updateObjectsRequests);
                            try
                            {
                                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                updateObjectsRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Object update failed", e);
                            }
                        }

                        if ((emptyQueue && updatePrimsRequests.Count > 0) || updatePrimsRequests.Count > 256)
                        {
                            string command = "REPLACE INTO prims (" + replaceIntoPrims + ") VALUES " + string.Join(",", updatePrimsRequests);
                            try
                            {
                                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                updatePrimsRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Prim update failed", e);
                            }
                        }

                        if ((emptyQueue && updatePrimItemsRequests.Count > 0) || updatePrimItemsRequests.Count > 256)
                        {
                            string command = "REPLACE INTO primitems (" + replaceIntoPrimItems + ") VALUES " + string.Join(",", updatePrimItemsRequests);
                            try
                            {
                                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                updatePrimItemsRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Prim inventory update failed", e);
                            }
                        }
                    }
                }
                finally
                {
                    m_SceneListenerThreads.Remove(this);
                }
            }

            private Dictionary<string, object> GenerateUpdateObjectPartInventoryItem(UUID primID, ObjectPartInventoryItem item)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("AssetId", item.AssetID);
                data.Add("AssetType", item.AssetType);
                data.Add("CreationDate", item.CreationDate);
                data.Add("Creator", item.Creator);
                data.Add("Description", item.Description);
                data.Add("Flags", item.Flags);
                data.Add("Group", item.Group);
                data.Add("GroupOwned", item.IsGroupOwned);
                data.Add("PrimID", primID);
                data.Add("Name", item.Name);
                data.Add("InventoryID", item.ID);
                data.Add("InventoryType", item.InventoryType);
                data.Add("LastOwner", item.LastOwner);
                data.Add("Owner", item.Owner);
                data.Add("ParentFolderID", item.ParentFolderID);
                data.Add("BasePermissions", item.Permissions.Base);
                data.Add("CurrentPermissions", item.Permissions.Current);
                data.Add("EveryOnePermissions", item.Permissions.EveryOne);
                data.Add("GroupPermissions", item.Permissions.Group);
                data.Add("NextOwnerPermissions", item.Permissions.NextOwner);
                data.Add("SaleType", item.SaleInfo.Type);
                data.Add("SalePrice", item.SaleInfo.Price);
                data.Add("SalePermMask", item.SaleInfo.PermMask);
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = item.PermsGranter;
                data.Add("PermsGranter", grantinfo.PermsGranter.ToString());
                data.Add("PermsMask", grantinfo.PermsMask);
                data.Add("NextOwnerAssetID", item.NextOwnerAssetID);
                return data;
            }

            private Dictionary<string, object> GenerateUpdateObjectGroup(ObjectGroup objgroup)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("ID", objgroup.ID);
                data.Add("RegionID", objgroup.Scene.ID);
                data.Add("IsTempOnRez", objgroup.IsTempOnRez);
                data.Add("Owner", objgroup.Owner);
                data.Add("LastOwner", objgroup.LastOwner);
                data.Add("Group", objgroup.Group);
                data.Add("OriginalAssetID", objgroup.OriginalAssetID);
                data.Add("NextOwnerAssetID", objgroup.NextOwnerAssetID);
                data.Add("SaleType", objgroup.SaleType);
                data.Add("SalePrice", objgroup.SalePrice);
                data.Add("PayPrice0", objgroup.PayPrice0);
                data.Add("PayPrice1", objgroup.PayPrice1);
                data.Add("PayPrice2", objgroup.PayPrice2);
                data.Add("PayPrice3", objgroup.PayPrice3);
                data.Add("PayPrice4", objgroup.PayPrice4);
                data.Add("AttachedPos", objgroup.AttachedPos);
                data.Add("AttachPoint", objgroup.AttachPoint);
                data.Add("IsIncludedInSearch", objgroup.IsIncludedInSearch);
                return data;
            }

            private Dictionary<string, object> GenerateUpdateObjectPart(ObjectPart objpart)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("ID", objpart.ID);
                data.Add("LinkNumber", objpart.LinkNumber);
                data.Add("RootPartID", objpart.ObjectGroup.RootPart.ID);
                data.Add("Position", objpart.Position);
                data.Add("Rotation", objpart.Rotation);
                data.Add("SitText", objpart.SitText);
                data.Add("TouchText", objpart.TouchText);
                data.Add("Name", objpart.Name);
                data.Add("Description", objpart.Description);
                data.Add("SitTargetOffset", objpart.SitTargetOffset);
                data.Add("SitTargetOrientation", objpart.SitTargetOrientation);
                data.Add("PhysicsShapeType", objpart.PhysicsShapeType);
                data.Add("PathfindingType", objpart.PathfindingType);
                data.Add("Material", objpart.Material);
                data.Add("Size", objpart.Size);
                data.Add("Slice", objpart.Slice);
                data.Add("MediaURL", objpart.MediaURL);
                data.Add("Creator", objpart.Creator);
                data.Add("CreationDate", objpart.CreationDate);
                data.Add("Flags", objpart.Flags);
                data.Add("AngularVelocity", objpart.AngularVelocity);
                data.Add("LightData", objpart.PointLight.Serialization);
                data.Add("HoverTextData", objpart.Text.Serialization);
                data.Add("FlexibleData", objpart.Flexible.Serialization);
                data.Add("LoopedSoundData", objpart.Sound.Serialization);
                data.Add("ImpactSoundData", objpart.CollisionSound.Serialization);
                data.Add("PrimitiveShapeData", objpart.Shape.Serialization);
                data.Add("ParticleSystem", objpart.ParticleSystemBytes);
                data.Add("TextureEntryBytes", objpart.TextureEntryBytes);
                data.Add("TextureAnimationBytes", objpart.TextureAnimationBytes);
                data.Add("ScriptAccessPin", objpart.ScriptAccessPin);
                data.Add("CameraAtOffset", objpart.CameraAtOffset);
                data.Add("CameraEyeOffset", objpart.CameraEyeOffset);
                data.Add("ForceMouselook", objpart.ForceMouselook);
                data.Add("BasePermissions", objpart.BaseMask);
                data.Add("CurrentPermissions", objpart.OwnerMask);
                data.Add("EveryOnePermissions", objpart.EveryoneMask);
                data.Add("GroupPermissions", objpart.GroupMask);
                data.Add("NextOwnerPermissions", objpart.NextOwnerMask);
                data.Add("ClickAction", objpart.ClickAction);

                using (MemoryStream ms = new MemoryStream())
                {
                    LlsdBinary.Serialize(objpart.DynAttrs, ms);
                    data.Add("DynAttrs", ms.ToArray());
                }

                data.Add("PassCollisionMode", objpart.PassCollisionMode);
                data.Add("PassTouchMode", objpart.PassTouchMode);
                data.Add("Velocity", objpart.Velocity);
                data.Add("IsSoundQueueing", objpart.IsSoundQueueing);
                data.Add("IsAllowedDrop", objpart.IsAllowedDrop);
                data.Add("PhysicsDensity", objpart.PhysicsDensity);
                data.Add("PhysicsFriction", objpart.PhysicsFriction);
                data.Add("PhysicsRestitution", objpart.PhysicsRestitution);
                data.Add("PhysicsGravityMultiplier", objpart.PhysicsGravityMultiplier);

                data.Add("IsRotateXEnabled", objpart.IsRotateXEnabled);
                data.Add("IsRotateYEnabled", objpart.IsRotateYEnabled);
                data.Add("IsRotateZEnabled", objpart.IsRotateZEnabled);
                data.Add("IsVolumeDetect", objpart.IsVolumeDetect);
                data.Add("IsPhantom", objpart.IsPhantom);
                data.Add("IsPhysics", objpart.IsPhysics);

                return data;
            }
        }

        public override SceneListener GetSceneListener(UUID regionID)
        {
            return new MySQLSceneListener(m_ConnectionString, regionID, m_SceneListenerThreads);
        }
    }
}
