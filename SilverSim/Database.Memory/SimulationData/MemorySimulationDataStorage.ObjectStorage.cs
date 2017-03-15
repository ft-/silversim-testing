// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Types.Script;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataObjectStorageInterface
    {
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> m_Objects = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>>(delegate () { return new RwLockedDictionary<UUID, Map>(); });
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> m_Primitives = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>>(delegate () { return new RwLockedDictionary<UUID, Map>(); });
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, Map>> m_PrimItems = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, Map>>(delegate () { return new RwLockedDictionary<string, Map>(); });

        internal static string GenItemKey(UUID primID, UUID itemID)
        {
            return primID.ToString() + ":" + itemID.ToString();
        }

        void RemoveAllObjectsInRegion(UUID key)
        {
            m_PrimItems.Remove(key);
            m_Primitives.Remove(key);
            m_Objects.Remove(key);
        }

        #region Objects and Prims within a region by UUID
        List<UUID> ISimulationDataObjectStorageInterface.ObjectsInRegion(UUID key)
        {
            RwLockedDictionary<UUID, Map> objects;
            if(m_Objects.TryGetValue(key, out objects))
            {
                return new List<UUID>(objects.Keys);
            }
            return new List<UUID>();
        }

        List<UUID> ISimulationDataObjectStorageInterface.PrimitivesInRegion(UUID key)
        {
            RwLockedDictionary<UUID, Map> objects;
            if (m_Primitives.TryGetValue(key, out objects))
            {
                return new List<UUID>(objects.Keys);
            }
            return new List<UUID>();
        }
        #endregion

        #region helpers
        ObjectGroup ObjectGroupFromMap(Map map)
        {
            ObjectGroup objgroup = new ObjectGroup();
            objgroup.IsTempOnRez = map["IsTempOnRez"].AsBoolean;
            objgroup.Owner = new UUI(map["Owner"].ToString());
            objgroup.LastOwner = new UUI(map["LastOwner"].ToString());
            objgroup.Group = new UGI(map["Group"].ToString());
            objgroup.SaleType = (InventoryItem.SaleInfoData.SaleType)map["SaleType"].AsUInt;
            objgroup.SalePrice = map["SalePrice"].AsInt;
            objgroup.PayPrice0 = map["PayPrice0"].AsInt;
            objgroup.PayPrice1 = map["PayPrice1"].AsInt;
            objgroup.PayPrice2 = map["PayPrice2"].AsInt;
            objgroup.PayPrice3 = map["PayPrice3"].AsInt;
            objgroup.PayPrice4 = map["PayPrice4"].AsInt;
            objgroup.AttachedPos = map["AttachedPos"].AsVector3;
            objgroup.AttachPoint = (AttachmentPoint)map["AttachPoint"].AsUInt;
            objgroup.IsIncludedInSearch = map["IsIncludedInSearch"].AsBoolean;
            return objgroup;
        }

        ObjectPart ObjectPartFromMap(Map map)
        {
            ObjectPart objpart = new ObjectPart();
            objpart.ID = map["ID"].AsUUID;
            objpart.LoadedLinkNumber = map["LinkNumber"].AsInt;
            objpart.Position = map["Position"].AsVector3;
            objpart.Rotation = map["Rotation"].AsQuaternion;
            objpart.SitText = map["SitText"].ToString();
            objpart.TouchText = map["TouchText"].ToString();
            objpart.Name = map["Name"].ToString();
            objpart.Description = map["Description"].ToString();
            objpart.SitTargetOffset = map["SitTargetOffset"].AsVector3;
            objpart.SitTargetOrientation = map["SitTargetOrientation"].AsQuaternion;
            objpart.Creator = new UUI(map["Creator"].ToString());
            objpart.CreationDate = (Date)map["CreationDate"];
            objpart.Flags = (PrimitiveFlags)map["Flags"].AsUInt;

            objpart.CameraAtOffset = map["CameraAtOffset"].AsVector3;
            objpart.CameraEyeOffset = map["CameraEyeOffset"].AsVector3;

            objpart.PhysicsShapeType = (PrimitivePhysicsShapeType)map["PhysicsShapeType"].AsInt;
            objpart.PathfindingType = (PathfindingType)map["PathfindingType"].AsInt;
            objpart.Material = (PrimitiveMaterial)map["Material"].AsInt;
            objpart.Size = map["Size"].AsVector3;
            objpart.Slice = map["Slice"].AsVector3;

            objpart.MediaURL = map["MediaURL"].ToString();

            objpart.AngularVelocity = map["AngularVelocity"].AsVector3;

            ObjectPart.PointLightParam lp = new ObjectPart.PointLightParam();
            lp.Serialization = (BinaryData)map["LightData"];
            objpart.PointLight = lp;

            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Serialization = (BinaryData)map["HoverTextData"];
            objpart.Text = tp;

            ObjectPart.FlexibleParam fp = new ObjectPart.FlexibleParam();
            fp.Serialization = (BinaryData)map["FlexibleData"];
            objpart.Flexible = fp;

            ObjectPart.SoundParam sound = new ObjectPart.SoundParam();
            sound.Serialization = (BinaryData)map["LoopedSoundData"];
            objpart.Sound = sound;

            ObjectPart.CollisionSoundParam collisionsound = new ObjectPart.CollisionSoundParam();
            collisionsound.Serialization = (BinaryData)map["ImpactSoundData"];
            objpart.CollisionSound = collisionsound;

            ObjectPart.PrimitiveShape ps = new ObjectPart.PrimitiveShape();
            ps.Serialization = (BinaryData)map["PrimitiveShapeData"];
            objpart.Shape = ps;

            objpart.ParticleSystemBytes = (BinaryData)map["ParticleSystem"];
            objpart.TextureEntryBytes = (BinaryData)map["TextureEntryBytes"];
            objpart.TextureAnimationBytes = (BinaryData)map["TextureAnimationBytes"];

            objpart.ScriptAccessPin = map["ScriptAccessPin"].AsInt;
            objpart.LoadedLinkNumber = map["LinkNumber"].AsInt;

            objpart.ForceMouselook = map["ForceMouselook"].AsBoolean;

            objpart.BaseMask = (InventoryPermissionsMask)map["BasePermissions"].AsUInt;
            objpart.OwnerMask = (InventoryPermissionsMask)map["CurrentPermissions"].AsUInt;
            objpart.EveryoneMask = (InventoryPermissionsMask)map["EveryOnePermissions"].AsUInt;
            objpart.GroupMask = (InventoryPermissionsMask)map["GroupPermissions"].AsUInt;
            objpart.NextOwnerMask = (InventoryPermissionsMask)map["NextOwnerPermissions"].AsUInt;

            objpart.ClickAction = (ClickActionType)map["ClickAction"].AsInt;

            using (MemoryStream ms = new MemoryStream((BinaryData)map["DynAttrs"]))
            {
                foreach (KeyValuePair<string, IValue> kvp in (Map)LlsdBinary.Deserialize(ms))
                {
                    objpart.DynAttrs.Add(kvp.Key, kvp.Value);
                }
            }

            objpart.PassCollisionMode = (PassEventMode)map["PassCollisionMode"].AsInt;
            objpart.PassTouchMode = (PassEventMode)map["PassTouchMode"].AsInt;
            objpart.Velocity = map["Velocity"].AsVector3;
            objpart.AngularVelocity = map["AngularVelocity"].AsVector3;
            objpart.IsSoundQueueing = map["IsSoundQueueing"].AsBoolean;
            objpart.IsAllowedDrop = map["IsAllowedDrop"].AsBoolean;

            objpart.PhysicsDensity = map["PhysicsDensity"].AsReal;
            objpart.PhysicsFriction = map["PhysicsFriction"].AsReal;
            objpart.PhysicsRestitution = map["PhysicsRestitution"].AsReal;
            objpart.PhysicsGravityMultiplier = map["PhysicsGravityMultiplier"].AsReal;

            objpart.IsRotateXEnabled = map["IsRotateXEnabled"].AsBoolean;
            objpart.IsRotateYEnabled = map["IsRotateYEnabled"].AsBoolean;
            objpart.IsRotateZEnabled = map["IsRotateZEnabled"].AsBoolean;
            objpart.IsVolumeDetect = map["IsVolumeDetect"].AsBoolean;
            objpart.IsPhantom = map["IsPhantom"].AsBoolean;
            objpart.IsPhysics = map["IsPhysics"].AsBoolean;

            return objpart;
        }

        ObjectPartInventoryItem ObjectPartInventoryItemFromMap(Map map)
        {
            ObjectPartInventoryItem item = new ObjectPartInventoryItem();
            item.AssetID = map["AssetID"].AsUUID;
            item.AssetType = (AssetType)map["AssetType"].AsInt;
            item.CreationDate = (Date)map["CreationDate"];
            item.Creator = new UUI(map["Creator"].ToString());
            item.Description = map["Description"].ToString();
            item.Flags = (InventoryFlags)map["Flags"].AsUInt;
            item.Group = new UGI(map["Group"].ToString());
            item.IsGroupOwned = map["GroupOwned"].AsBoolean;
            item.ID = map["InventoryID"].AsUUID;
            item.InventoryType = (InventoryType)map["InventoryType"].AsInt;
            item.LastOwner = new UUI(map["LastOwner"].ToString());
            item.Name = map["Name"].ToString();
            item.Owner = new UUI(map["Owner"].ToString());
            item.ParentFolderID = map["ParentFolderID"].AsUUID;
            item.Permissions.Base = (InventoryPermissionsMask)map["BasePermissions"].AsUInt;
            item.Permissions.Current = (InventoryPermissionsMask)map["CurrentPermissions"].AsUInt;
            item.Permissions.EveryOne = (InventoryPermissionsMask)map["EveryOnePermissions"].AsUInt;
            item.Permissions.Group = (InventoryPermissionsMask)map["GroupPermissions"].AsUInt;
            item.Permissions.NextOwner = (InventoryPermissionsMask)map["NextOwnerPermissions"].AsUInt;
            item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType)map["SaleType"].AsUInt;
            item.SaleInfo.Price = map["SalePrice"].AsInt;
            item.SaleInfo.PermMask = (InventoryPermissionsMask)map["SalePermMask"].AsInt;
            item.NextOwnerAssetID = map["NextOwnerAssetID"].AsUUID;
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
            if ((map["PermsGranter"].ToString()).Length != 0)
            {
                try
                {
                    grantinfo.PermsGranter = new UUI(map["PermsGranter"].ToString());
                }
                catch
                {
                    /* no action required */
                }
            }
            grantinfo.PermsMask = (ScriptPermissions)map["PermsMask"].AsUInt;

            return item;
        }
        #endregion

        #region Load all object groups of a single region
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        List<ObjectGroup> ISimulationDataObjectStorageInterface.this[UUID regionID]
        {
            get
            {
                Dictionary<UUID, ObjectGroup> objGroups = new Dictionary<UUID, ObjectGroup>();
                Dictionary<UUID, UUID> originalAssetIDs = new Dictionary<UUID, UUID>();
                Dictionary<UUID, UUID> nextOwnerAssetIDs = new Dictionary<UUID, UUID>();
                Dictionary<UUID, SortedDictionary<int, ObjectPart>> objGroupParts = new Dictionary<UUID, SortedDictionary<int, ObjectPart>>();
                List<UUID> objPartIDs = new List<UUID>();
                Dictionary<UUID, ObjectPart> objParts = new Dictionary<UUID,ObjectPart>();
                List<UUID> orphanedPrims = new List<UUID>();
                List<KeyValuePair<UUID, UUID>> orphanedPrimInventories = new List<KeyValuePair<UUID, UUID>>();

                RwLockedDictionary<UUID, Map> objectList = new RwLockedDictionary<UUID, Map>();
                RwLockedDictionary<UUID, Map> primitiveList = new RwLockedDictionary<UUID, Map>();
                RwLockedDictionary<string, Map> primItemList = new RwLockedDictionary<string, Map>();

                if (m_Objects.TryGetValue(regionID, out objectList))
                {
                    UUID objgroupID = UUID.Zero;
                    m_Log.InfoFormat("Loading object groups for region ID {0}", regionID);

                    foreach(KeyValuePair<UUID, Map> kvp in objectList)
                    {
                        try
                        {
                            objgroupID = kvp.Key;
                            originalAssetIDs[objgroupID] = kvp.Value["OriginalAssetID"].AsUUID;
                            nextOwnerAssetIDs[objgroupID] = kvp.Value["NextOwnerAssetID"].AsUUID;
                            ObjectGroup objgroup = ObjectGroupFromMap(kvp.Value);
                            objGroups[objgroupID] = objgroup;
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("Failed to load object {0}: {1}\n{2}", objgroupID, e.Message, e.StackTrace);
                            objGroups.Remove(objgroupID);
                        }

                    }

                    m_Log.InfoFormat("Loading prims for region ID {0}", regionID);
                    int primcount = 0;
                    if(m_Primitives.TryGetValue(regionID, out primitiveList))
                    {
                        foreach(KeyValuePair<UUID, Map> kvp in primitiveList)
                        {
                            UUID rootPartID = kvp.Value["RootPartID"].AsUUID;
                            if (objGroups.ContainsKey(rootPartID))
                            {
                                if(!objGroupParts.ContainsKey(rootPartID))
                                {
                                    objGroupParts.Add(rootPartID, new SortedDictionary<int, ObjectPart>());
                                }

                                ObjectPart objpart = ObjectPartFromMap(kvp.Value);

                                objGroupParts[rootPartID].Add(objpart.LoadedLinkNumber, objpart);
                                objPartIDs.Add(objpart.ID);
                                objParts[objpart.ID] = objpart;
                                if ((++primcount) % 5000 == 0)
                                {
                                    m_Log.InfoFormat("Loading prims for region ID {0} - {1} loaded", regionID, primcount);
                                }
                            }
                            else
                            {
                                m_Log.WarnFormat("deleting orphan prim in region ID {0}: {1}", regionID, kvp.Key);
                                orphanedPrims.Add(kvp.Key);
                            }
                        }
                    }
                    m_Log.InfoFormat("Loaded prims for region ID {0} - {1} loaded", regionID, primcount);

                    int primitemcount = 0;
                    m_Log.InfoFormat("Loading prim inventories for region ID {0}", regionID);
                    if(m_PrimItems.TryGetValue(regionID, out primItemList))
                    {
                        foreach(Map map in primItemList.Values)
                        {
                            UUID partID = map["PrimID"].AsUUID;
                            ObjectPart part;
                            if (objParts.TryGetValue(partID, out part))
                            {
                                ObjectPartInventoryItem item = ObjectPartInventoryItemFromMap(map);

                                part.Inventory.Add(item.ID, item.Name, item);
                                if ((++primitemcount) % 5000 == 0)
                                {
                                    m_Log.InfoFormat("Loading prim inventories for region ID {0} - {1} loaded", regionID, primitemcount);
                                }
                            }
                            else
                            {
                                m_Log.WarnFormat("deleting orphan prim in region ID {0}: {1}", regionID, map["ID"].AsUUID);
                                orphanedPrimInventories.Add(new KeyValuePair<UUID, UUID>(map["PrimID"].AsUUID, map["ID"].AsUUID));
                            }
                        }
                    }
                    m_Log.InfoFormat("Loaded prim inventories for region ID {0} - {1} loaded", regionID, primitemcount);
                }

                List<UUID> removeObjGroups = new List<UUID>();
                foreach(KeyValuePair<UUID, ObjectGroup> kvp in objGroups)
                {
                    if (!objGroupParts.ContainsKey(kvp.Key))
                    {
                        removeObjGroups.Add(kvp.Key);
                    }
                    else
                    {
                        foreach (ObjectPart objpart in objGroupParts[kvp.Key].Values)
                        {
                            kvp.Value.Add(objpart.LoadedLinkNumber, objpart.ID, objpart);
                        }

                        try
                        {
                            kvp.Value.OriginalAssetID = originalAssetIDs[kvp.Value.ID];
                            kvp.Value.NextOwnerAssetID = nextOwnerAssetIDs[kvp.Value.ID];
                            kvp.Value.FinalizeObject();
                        }
                        catch
                        {
                            m_Log.WarnFormat("deleting orphan object in region ID {0}: {1}", regionID, kvp.Key);
                            removeObjGroups.Add(kvp.Key);
                        }
                    }
                }

                foreach(UUID objid in removeObjGroups)
                {
                    objGroups.Remove(objid);
                    objectList.Remove(objid);
                }

                foreach(UUID primid in orphanedPrims)
                {
                    primitiveList.Remove(primid);
                }

                foreach(KeyValuePair<UUID, UUID> kvp in orphanedPrimInventories)
                {
                    primItemList.Remove(GenItemKey(kvp.Key, kvp.Value));
                }

                return new List<ObjectGroup>(objGroups.Values);
            }
        }
        #endregion
    }
}
