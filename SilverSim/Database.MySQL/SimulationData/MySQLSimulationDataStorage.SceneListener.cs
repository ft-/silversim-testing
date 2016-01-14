// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage
    {
        protected override void StorageMainThread()
        {
            Thread.CurrentThread.Name = "Storage Main Thread";
            Dictionary<UUID, string> primDeletionRequests = new Dictionary<UUID, string>();
            Dictionary<UUID, string> primItemDeletionRequests = new Dictionary<UUID, string>();
            Dictionary<UUID, string> objectDeletionRequests = new Dictionary<UUID, string>();
            Dictionary<UUID, string> updateObjectsRequests = new Dictionary<UUID, string>();
            Dictionary<UUID, string> updatePrimsRequests = new Dictionary<UUID, string>();
            Dictionary<UUID, string> updatePrimItemsRequests = new Dictionary<UUID, string>();
            Dictionary<uint, int> knownSerialNumbers = new Dictionary<uint, int>();
            Dictionary<uint, int> knownInventorySerialNumbers = new Dictionary<uint, int>();
            Dictionary<uint, List<UUID>> knownInventories = new Dictionary<uint, List<UUID>>();
            Dictionary<uint, bool> knownRootPrim = new Dictionary<uint, bool>();
            string replaceIntoObjects = string.Empty;
            string replaceIntoPrims = string.Empty;
            string replaceIntoPrimItems = string.Empty;
            int m_ProcessedPrims = 0;

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
                    primDeletionRequests.Add(req.Part.ID, string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", req.Part.ObjectGroup.Scene.ID, req.Part.ID));
                    primItemDeletionRequests.Add(req.Part.ID, string.Format("(RegionID LIKE '{0}' AND PrimID LIKE '{1}')", req.Part.ObjectGroup.Scene.ID, req.Part.ID));
                    if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                    {
                        objectDeletionRequests.Add(req.Part.ID, string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", req.Part.ObjectGroup.Scene.ID, req.Part.ID));
                    }
                }
                else if (req.Part.SerialNumberLoadedFromDatabase == serialNumber)
                {
                    req.Part.SerialNumberLoadedFromDatabase = 0;
                    if (serialNumber == req.SerialNumber)
                    {
                        /* ignore those */
                        knownInventorySerialNumbers[req.LocalID] = req.Part.Inventory.InventorySerial;
                        knownInventories[req.LocalID] = req.Part.Inventory.Keys1;
                        knownSerialNumbers[req.LocalID] = serialNumber;
                        continue;
                    }
                }
                else if (knownSerialNumbers.TryGetValue(req.LocalID, out knownSerial))
                {
                    if (req.Part.ObjectGroup.IsAttached || req.Part.ObjectGroup.IsTemporary)
                    {
                        primDeletionRequests.Add(req.Part.ID, string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", req.Part.ObjectGroup.Scene.ID, req.Part.ID));
                        primItemDeletionRequests.Add(req.Part.ID, string.Format("(RegionID LIKE '{0}' AND PrimID LIKE '{1}')", req.Part.ObjectGroup.Scene.ID, req.Part.ID));
                        if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                        {
                            objectDeletionRequests.Add(req.Part.ID, string.Format("(RegionID LIKE '{0}' AND ID LIKE '{1}')", req.Part.ObjectGroup.Scene.ID, req.Part.ID));
                        }
                    }
                    else
                    {
                        if (knownSerial != req.SerialNumber && !req.Part.ObjectGroup.IsAttached && !req.Part.ObjectGroup.IsTemporary)
                        {
                            /* prim update */
                            updatePrim = true;
                            updateInventory = true;
                            primDeletionRequests.Remove(req.Part.ID);
                            primItemDeletionRequests.Remove(req.Part.ID);
                            if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                            {
                                objectDeletionRequests.Remove(req.Part.ID);
                            }
                        }

                        if (knownInventorySerialNumbers.TryGetValue(req.LocalID, out knownInventorySerial) &&
                            knownInventorySerial != req.Part.Inventory.InventorySerial)
                        {
                            /* inventory update */
                            updateInventory = true;
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
                    primDeletionRequests.Remove(req.Part.ID);
                    primItemDeletionRequests.Remove(req.Part.ID);
                    if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                    {
                        objectDeletionRequests.Remove(req.Part.ID);
                    }
                }

                int newPrimSerial = req.Part.SerialNumber;
                int newPrimInventorySerial = req.Part.Inventory.InventorySerial;
                bool rootPrim = req.Part.LinkNumber == ObjectGroup.LINK_ROOT;

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
                    updatePrimsRequests.Add(req.Part.ID, "(" + MySQLUtilities.GenerateValues(primData) + ")");

                    Dictionary<string, object> objData = GenerateUpdateObjectGroup(grp);
                    if(replaceIntoObjects.Length == 0)
                    {
                        replaceIntoObjects = MySQLUtilities.GenerateFieldNames(objData);
                    }
                    updateObjectsRequests.Add(req.Part.ID, "(" + MySQLUtilities.GenerateValues(objData) + ")");
                }

                if(updateInventory)
                {

                }

                bool emptyQueue = m_StorageMainRequestQueue.Count == 0;

                if((emptyQueue && objectDeletionRequests.Count > 0) || objectDeletionRequests.Count > 256)
                {
                    string elems = string.Join(" OR ", objectDeletionRequests.Values);
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

                if ((emptyQueue && primDeletionRequests.Count > 0) || primDeletionRequests.Count > 256)
                {
                    string elems = string.Join(" OR ", primDeletionRequests.Values);
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
                    }
                    catch (Exception e)
                    {
                        m_Log.Error("Prim deletion failed", e);
                    }
                }

                if ((emptyQueue && primItemDeletionRequests.Count > 0) || primItemDeletionRequests.Count > 256)
                {
                    string elems = string.Join(" OR ", primItemDeletionRequests.Values);
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
                        primDeletionRequests.Clear();
                    }
                    catch(Exception e)
                    {
                        m_Log.Error("Object deletion failed", e);
                    }
                }

                if ((emptyQueue && updateObjectsRequests.Count > 0) || updateObjectsRequests.Count > 256)
                {
                    string command = "REPLACE INTO objects (" + replaceIntoObjects + ") VALUES " + string.Join(",", updateObjectsRequests.Values);
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
                    catch(Exception e)
                    {
                        m_Log.Error("Object update failed", e);
                    }
                }

                if ((emptyQueue && updatePrimsRequests.Count > 0) || updatePrimsRequests.Count > 256)
                {
                    string command = "REPLACE INTO prims (" + replaceIntoPrims + ") VALUES " + string.Join(",", updatePrimsRequests.Values);
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

                }
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
            return data;
        }

        private Dictionary<string, object> GenerateUpdateObjectGroup(ObjectGroup objgroup)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("ID", objgroup.ID);
            data.Add("RegionID", objgroup.Scene.ID);
            data.Add("IsVolumeDetect", objgroup.IsVolumeDetect);
            data.Add("IsPhantom", objgroup.IsPhantom);
            data.Add("IsPhysics", objgroup.IsPhysics);
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

            using (MemoryStream ms = new MemoryStream())
            {
                LlsdBinary.Serialize(objpart.DynAttrs, ms);
                data.Add("DynAttrs", ms.GetBuffer());
            }

            return data;
        }

    }
}
