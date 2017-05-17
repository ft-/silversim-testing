// SilverSim is distributed under the terms of the
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

using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage
    {
        public class MemorySceneListener : SceneListener
        {
            readonly UUID m_RegionID;
            internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> m_Objects;
            internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> m_Primitives;
            internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, Map>> m_PrimItems;

            public MemorySceneListener(
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> objects,
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> primitives,
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, Map>> primitems,
                UUID regionID)
            {
                m_Objects = objects;
                m_Primitives = primitives;
                m_PrimItems = primitems;
                m_RegionID = regionID;
            }

            protected override void StorageMainThread()
            {
                Thread.CurrentThread.Name = "Storage Main Thread: " + m_RegionID.ToString();

                var knownSerialNumbers = new C5.TreeDictionary<uint, int>();
                var knownInventorySerialNumbers = new C5.TreeDictionary<uint, int>();
                var knownInventories = new C5.TreeDictionary<uint, List<UUID>>();

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
                        string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                        string partID = req.Part.ID.ToString();
                        RwLockedDictionary<UUID, Map> primitiveList;
                        RwLockedDictionary<string, Map> primItemList;
                        if(m_Primitives.TryGetValue(sceneID, out primitiveList))
                        {
                            primitiveList.Remove(partID);
                        }

                        if(m_PrimItems.TryGetValue(sceneID, out primItemList))
                        {
                            var deleteItems = new List<string>(from id in primItemList.Keys where id.StartsWith(partID.ToString()) select id);
                            foreach(string id in deleteItems)
                            {
                                m_PrimItems.Remove(id);
                            }
                        }
                        knownSerialNumbers.Remove(req.LocalID);
                        if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                        {
                            RwLockedDictionary<UUID, Map> objectList;
                            if(m_Objects.TryGetValue(sceneID, out objectList))
                            {
                                objectList.Remove(partID);
                            }
                        }
                    }
                    else if (knownSerialNumbers.Contains(req.LocalID))
                    {
                        knownSerial = knownSerialNumbers[req.LocalID];
                        if (req.Part.ObjectGroup.IsAttached || req.Part.ObjectGroup.IsTemporary)
                        {
                            string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                            string partID = req.Part.ID.ToString();
                            m_Primitives[sceneID].Remove(partID);

                            RwLockedDictionary<string, Map> primItemList;
                            if (m_PrimItems.TryGetValue(sceneID, out primItemList))
                            {
                                var deleteKeys = new List<string>(from id in primItemList.Keys where id.StartsWith(partID) select id);
                                foreach(string key in deleteKeys)
                                {
                                    primItemList.Remove(key);
                                }
                            }
                            if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                            {
                                m_Objects[sceneID].Remove(partID);
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
                        var primData = GenerateUpdateObjectPart(req.Part);
                        var grp = req.Part.ObjectGroup;
                        primData.Add("RegionID", grp.Scene.ID);
                        m_Primitives[grp.Scene.ID][req.Part.ID] = primData;
                        knownSerialNumbers[req.LocalID] = req.SerialNumber;

                        m_Objects[grp.Scene.ID][grp.ID] = GenerateUpdateObjectGroup(grp);
                    }

                    if (updateInventory)
                    {
                        var items = new Dictionary<UUID, ObjectPartInventoryItem>();
                        foreach (var item in req.Part.Inventory.ValuesByKey1)
                        {
                            items.Add(item.ID, item);
                        }

                        if (knownInventories.Contains(req.Part.LocalID))
                        {
                            string partID = req.Part.ID.ToString();
                            foreach (var itemID in knownInventories[req.Part.LocalID])
                            {
                                if (!items.ContainsKey(itemID))
                                {
                                    m_PrimItems[req.Part.ObjectGroup.Scene.ID].Remove(GenItemKey(partID, itemID.ToString()));
                                }
                            }

                            foreach (var kvp in items)
                            {
                                var data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
                                data["RegionID"] = req.Part.ObjectGroup.Scene.ID;
                                m_PrimItems[req.Part.ObjectGroup.Scene.ID][GenItemKey(req.Part.ID, kvp.Key)] = data;
                            }
                        }
                        else
                        {
                            foreach (var kvp in items)
                            {
                                var data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
                                data["RegionID"] = req.Part.ObjectGroup.Scene.ID;
                                m_PrimItems[req.Part.ObjectGroup.Scene.ID][GenItemKey(req.Part.ID, kvp.Key)] = data;
                            }
                        }
                        knownInventories[req.Part.LocalID] = new List<UUID>(items.Keys);
                        knownInventorySerialNumbers[req.Part.LocalID] = newPrimInventorySerial;
                    }
                }
            }

            private Map GenerateUpdateObjectPartInventoryItem(UUID primID, ObjectPartInventoryItem item)
            {
                var data = new Map
                {
                    { "AssetId", item.AssetID },
                    { "AssetType", (int)item.AssetType },
                    { "CreationDate", item.CreationDate },
                    { "Creator", item.Creator.ToString() },
                    { "Description", item.Description },
                    { "Flags", (int)item.Flags },
                    { "Group", item.Group.ToString() },
                    { "GroupOwned", item.IsGroupOwned },
                    { "PrimID", primID },
                    { "Name", item.Name },
                    { "InventoryID", item.ID },
                    { "InventoryType", (int)item.InventoryType },
                    { "LastOwner", item.LastOwner.ToString() },
                    { "Owner", item.Owner.ToString() },
                    { "ParentFolderID", item.ParentFolderID },
                    { "BasePermissions", (int)item.Permissions.Base },
                    { "CurrentPermissions", (int)item.Permissions.Current },
                    { "EveryOnePermissions", (int)item.Permissions.EveryOne },
                    { "GroupPermissions", (int)item.Permissions.Group },
                    { "NextOwnerPermissions", (int)item.Permissions.NextOwner },
                    { "SaleType", (int)item.SaleInfo.Type },
                    { "SalePrice", item.SaleInfo.Price },
                    { "SalePermMask", (int)item.SaleInfo.PermMask }
                };
                var grantinfo = item.PermsGranter;
                data.Add("PermsGranter", grantinfo.PermsGranter.ToString());
                data.Add("PermsMask", (int)grantinfo.PermsMask);
                data.Add("NextOwnerAssetID", item.NextOwnerAssetID);
                return data;
            }

            private Map GenerateUpdateObjectGroup(ObjectGroup objgroup)
            {
                return new Map
                {
                    { "ID", objgroup.ID },
                    { "RegionID", objgroup.Scene.ID },
                    { "IsTempOnRez", objgroup.IsTempOnRez },
                    { "Owner", objgroup.Owner.ToString() },
                    { "LastOwner", objgroup.LastOwner.ToString() },
                    { "Group", objgroup.Group.ToString() },
                    { "OriginalAssetID", objgroup.OriginalAssetID },
                    { "NextOwnerAssetID", objgroup.NextOwnerAssetID },
                    { "SaleType", (int)objgroup.SaleType },
                    { "SalePrice", objgroup.SalePrice },
                    { "PayPrice0", objgroup.PayPrice0 },
                    { "PayPrice1", objgroup.PayPrice1 },
                    { "PayPrice2", objgroup.PayPrice2 },
                    { "PayPrice3", objgroup.PayPrice3 },
                    { "PayPrice4", objgroup.PayPrice4 },
                    { "AttachedPos", objgroup.AttachedPos },
                    { "AttachPoint", (int)objgroup.AttachPoint },
                    { "IsIncludedInSearch", objgroup.IsIncludedInSearch },
                    { "RezzingObjectID", objgroup.RezzingObjectID }
                };
            }

            private Map GenerateUpdateObjectPart(ObjectPart objpart)
            {
                var data = new Map
                {
                    { "ID", objpart.ID },
                    { "LinkNumber", objpart.LinkNumber },
                    { "RootPartID", objpart.ObjectGroup.RootPart.ID },
                    { "Position", objpart.Position },
                    { "Rotation", objpart.Rotation },
                    { "SitText", objpart.SitText },
                    { "TouchText", objpart.TouchText },
                    { "Name", objpart.Name },
                    { "Description", objpart.Description },
                    { "SitTargetOffset", objpart.SitTargetOffset },
                    { "SitTargetOrientation", objpart.SitTargetOrientation },
                    { "PhysicsShapeType", (int)objpart.PhysicsShapeType },
                    { "PathfindingType", (int)objpart.PathfindingType },
                    { "Material", (int)objpart.Material },
                    { "Size", objpart.Size },
                    { "Slice", objpart.Slice },
                    { "MediaURL", objpart.MediaURL },
                    { "Creator", objpart.Creator.ToString() },
                    { "CreationDate", objpart.CreationDate },
                    { "Flags", (int)objpart.Flags },
                    { "AngularVelocity", objpart.AngularVelocity },
                    { "LightData", new BinaryData(objpart.PointLight.Serialization) },
                    { "HoverTextData", new BinaryData(objpart.Text.Serialization) },
                    { "FlexibleData", new BinaryData(objpart.Flexible.Serialization) },
                    { "LoopedSoundData", new BinaryData(objpart.Sound.Serialization) },
                    { "ImpactSoundData", new BinaryData(objpart.CollisionSound.Serialization) },
                    { "PrimitiveShapeData", new BinaryData(objpart.Shape.Serialization) },
                    { "ParticleSystem", new BinaryData(objpart.ParticleSystemBytes) },
                    { "TextureEntryBytes", new BinaryData(objpart.TextureEntryBytes) },
                    { "TextureAnimationBytes", new BinaryData(objpart.TextureAnimationBytes) },
                    { "ScriptAccessPin", objpart.ScriptAccessPin },
                    { "CameraAtOffset", objpart.CameraAtOffset },
                    { "CameraEyeOffset", objpart.CameraEyeOffset },
                    { "ForceMouselook", objpart.ForceMouselook },
                    { "BasePermissions", (int)objpart.BaseMask },
                    { "CurrentPermissions", (int)objpart.OwnerMask },
                    { "EveryOnePermissions", (int)objpart.EveryoneMask },
                    { "GroupPermissions", (int)objpart.GroupMask },
                    { "NextOwnerPermissions", (int)objpart.NextOwnerMask },
                    { "ClickAction", (int)objpart.ClickAction }
                };
                using (var ms = new MemoryStream())
                {
                    LlsdBinary.Serialize(objpart.DynAttrs, ms);
                    data.Add("DynAttrs", new BinaryData(ms.ToArray()));
                }

                data.Add("PassCollisionMode", (int)objpart.PassCollisionMode);
                data.Add("PassTouchMode", (int)objpart.PassTouchMode);
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
            return new MemorySceneListener(m_Objects, m_Primitives, m_PrimItems, regionID);
        }
    }
}
