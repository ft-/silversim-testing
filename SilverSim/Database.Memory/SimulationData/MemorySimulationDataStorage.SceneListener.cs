// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

                C5.TreeDictionary<uint, int> knownSerialNumbers = new C5.TreeDictionary<uint, int>();
                C5.TreeDictionary<uint, int> knownInventorySerialNumbers = new C5.TreeDictionary<uint, int>();
                C5.TreeDictionary<uint, List<UUID>> knownInventories = new C5.TreeDictionary<uint, List<UUID>>();

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
                            List<string> deleteItems = new List<string>(from id in primItemList.Keys where id.StartsWith(partID.ToString()) select id);
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
                                List<string> deleteKeys = new List<string>(from id in primItemList.Keys where id.StartsWith(partID.ToString()) select id);
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
                        Map primData = GenerateUpdateObjectPart(req.Part);
                        ObjectGroup grp = req.Part.ObjectGroup;
                        primData.Add("RegionID", grp.Scene.ID);
                        m_Primitives[grp.Scene.ID][req.Part.ID] = primData;
                        knownSerialNumbers[req.LocalID] = req.SerialNumber;

                        m_Objects[grp.Scene.ID][grp.ID] = GenerateUpdateObjectGroup(grp);
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
                                    m_PrimItems[req.Part.ObjectGroup.Scene.ID].Remove(GenItemKey(partID, itemID.ToString()));
                                }
                            }

                            foreach (KeyValuePair<UUID, ObjectPartInventoryItem> kvp in items)
                            {
                                Map data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
                                data["RegionID"] = req.Part.ObjectGroup.Scene.ID;
                                m_PrimItems[req.Part.ObjectGroup.Scene.ID][GenItemKey(req.Part.ID, kvp.Key)] = data;
                            }
                        }
                        else
                        {
                            foreach (KeyValuePair<UUID, ObjectPartInventoryItem> kvp in items)
                            {
                                Map data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
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
                Map data = new Map();
                data.Add("AssetId", item.AssetID);
                data.Add("AssetType", (int)item.AssetType);
                data.Add("CreationDate", item.CreationDate);
                data.Add("Creator", item.Creator.ToString());
                data.Add("Description", item.Description);
                data.Add("Flags", (int)item.Flags);
                data.Add("Group", item.Group.ToString());
                data.Add("GroupOwned", item.IsGroupOwned);
                data.Add("PrimID", primID);
                data.Add("Name", item.Name);
                data.Add("InventoryID", item.ID);
                data.Add("InventoryType", (int)item.InventoryType);
                data.Add("LastOwner", item.LastOwner.ToString());
                data.Add("Owner", item.Owner.ToString());
                data.Add("ParentFolderID", item.ParentFolderID);
                data.Add("BasePermissions", (int)item.Permissions.Base);
                data.Add("CurrentPermissions", (int)item.Permissions.Current);
                data.Add("EveryOnePermissions", (int)item.Permissions.EveryOne);
                data.Add("GroupPermissions", (int)item.Permissions.Group);
                data.Add("NextOwnerPermissions", (int)item.Permissions.NextOwner);
                data.Add("SaleType", (int)item.SaleInfo.Type);
                data.Add("SalePrice", item.SaleInfo.Price);
                data.Add("SalePermMask", (int)item.SaleInfo.PermMask);
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = item.PermsGranter;
                data.Add("PermsGranter", grantinfo.PermsGranter.ToString());
                data.Add("PermsMask", (int)grantinfo.PermsMask);
                data.Add("NextOwnerAssetID", item.NextOwnerAssetID);
                return data;
            }

            private Map GenerateUpdateObjectGroup(ObjectGroup objgroup)
            {
                Map data = new Map();
                data.Add("ID", objgroup.ID);
                data.Add("RegionID", objgroup.Scene.ID);
                data.Add("IsVolumeDetect", objgroup.IsVolumeDetect);
                data.Add("IsPhantom", objgroup.IsPhantom);
                data.Add("IsPhysics", objgroup.IsPhysics);
                data.Add("IsTempOnRez", objgroup.IsTempOnRez);
                data.Add("Owner", objgroup.Owner.ToString());
                data.Add("LastOwner", objgroup.LastOwner.ToString());
                data.Add("Group", objgroup.Group.ToString());
                data.Add("OriginalAssetID", objgroup.OriginalAssetID);
                data.Add("NextOwnerAssetID", objgroup.NextOwnerAssetID);
                data.Add("SaleType", (int)objgroup.SaleType);
                data.Add("SalePrice", objgroup.SalePrice);
                data.Add("PayPrice0", objgroup.PayPrice0);
                data.Add("PayPrice1", objgroup.PayPrice1);
                data.Add("PayPrice2", objgroup.PayPrice2);
                data.Add("PayPrice3", objgroup.PayPrice3);
                data.Add("PayPrice4", objgroup.PayPrice4);
                data.Add("AttachedPos", objgroup.AttachedPos);
                data.Add("AttachPoint", (int)objgroup.AttachPoint);
                data.Add("IsIncludedInSearch", objgroup.IsIncludedInSearch);
                return data;
            }

            private Map GenerateUpdateObjectPart(ObjectPart objpart)
            {
                Map data = new Map();
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
                data.Add("PhysicsShapeType", (int)objpart.PhysicsShapeType);
                data.Add("Material", (int)objpart.Material);
                data.Add("Size", objpart.Size);
                data.Add("Slice", objpart.Slice);
                data.Add("MediaURL", objpart.MediaURL);
                data.Add("Creator", objpart.Creator.ToString());
                data.Add("CreationDate", objpart.CreationDate);
                data.Add("Flags", (int)objpart.Flags);
                data.Add("AngularVelocity", objpart.AngularVelocity);
                data.Add("LightData", new BinaryData(objpart.PointLight.Serialization));
                data.Add("HoverTextData", new BinaryData(objpart.Text.Serialization));
                data.Add("FlexibleData", new BinaryData(objpart.Flexible.Serialization));
                data.Add("LoopedSoundData", new BinaryData(objpart.Sound.Serialization));
                data.Add("ImpactSoundData", new BinaryData(objpart.CollisionSound.Serialization));
                data.Add("PrimitiveShapeData", new BinaryData(objpart.Shape.Serialization));
                data.Add("ParticleSystem", new BinaryData(objpart.ParticleSystemBytes));
                data.Add("TextureEntryBytes", new BinaryData(objpart.TextureEntryBytes));
                data.Add("TextureAnimationBytes", new BinaryData(objpart.TextureAnimationBytes));
                data.Add("ScriptAccessPin", objpart.ScriptAccessPin);
                data.Add("CameraAtOffset", objpart.CameraAtOffset);
                data.Add("CameraEyeOffset", objpart.CameraEyeOffset);
                data.Add("ForceMouselook", objpart.ForceMouselook);
                data.Add("BasePermissions", (int)objpart.BaseMask);
                data.Add("CurrentPermissions", (int)objpart.OwnerMask);
                data.Add("EveryOnePermissions", (int)objpart.EveryoneMask);
                data.Add("GroupPermissions", (int)objpart.GroupMask);
                data.Add("NextOwnerPermissions", (int)objpart.NextOwnerMask);
                data.Add("ClickAction", (int)objpart.ClickAction);

                using (MemoryStream ms = new MemoryStream())
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

                return data;
            }
        }

        public override SceneListener GetSceneListener(UUID regionID)
        {
            return new MemorySceneListener(m_Objects, m_Primitives, m_PrimItems, regionID);
        }
    }
}
