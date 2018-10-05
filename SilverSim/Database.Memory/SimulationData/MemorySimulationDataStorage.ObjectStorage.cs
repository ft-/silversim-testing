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

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Physics.Vehicle;
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
using System.IO;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataObjectStorageInterface
    {
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> m_Objects = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>>(() => new RwLockedDictionary<UUID, Map>());
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> m_Primitives = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>>(() => new RwLockedDictionary<UUID, Map>());
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, Map>> m_PrimItems = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, Map>>(() => new RwLockedDictionary<string, Map>());

        internal static string GenItemKey(UUID primID, UUID itemID) =>
            primID.ToString() + ":" + itemID.ToString();

        private void RemoveAllObjectsInRegion(UUID key)
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
        private ObjectGroup ObjectGroupFromMap(Map map) => new ObjectGroup
        {
            IsTemporary = map["IsTemporary"].AsBoolean,
            Owner = new UGUI(map["Owner"].ToString()),
            LastOwner = new UGUI(map["LastOwner"].ToString()),
            Group = new UGI(map["Group"].ToString()),
            SaleType = (InventoryItem.SaleInfoData.SaleType)map["SaleType"].AsUInt,
            SalePrice = map["SalePrice"].AsInt,
            PayPrice0 = map["PayPrice0"].AsInt,
            PayPrice1 = map["PayPrice1"].AsInt,
            PayPrice2 = map["PayPrice2"].AsInt,
            PayPrice3 = map["PayPrice3"].AsInt,
            PayPrice4 = map["PayPrice4"].AsInt,
            AttachedPos = map["AttachedPos"].AsVector3,
            AttachedRot = map["AttachedRot"].AsQuaternion,
            AttachPoint = (AttachmentPoint)map["AttachPoint"].AsUInt,
            IsIncludedInSearch = map["IsIncludedInSearch"].AsBoolean,
            RezzingObjectID = map["RezzingObjectID"].AsUUID
        };

        private ObjectPart ObjectPartFromMap(Map map)
        {
            var objpart = new ObjectPart(map["ID"].AsUUID)
            {
                LoadedLinkNumber = map["LinkNumber"].AsInt,
                Position = map["Position"].AsVector3,
                Rotation = map["Rotation"].AsQuaternion,
                SitText = map["SitText"].ToString(),
                TouchText = map["TouchText"].ToString(),
                Name = map["Name"].ToString(),
                Description = map["Description"].ToString(),
                SitTargetOffset = map["SitTargetOffset"].AsVector3,
                SitTargetOrientation = map["SitTargetOrientation"].AsQuaternion,
                SitAnimation = map["SitAnimation"].ToString(),
                Creator = new UGUI(map["Creator"].ToString()),
                CreationDate = (Date)map["CreationDate"],
                RezDate = (Date)map["RezDate"],
                Flags = (PrimitiveFlags)map["Flags"].AsUInt,

                CameraAtOffset = map["CameraAtOffset"].AsVector3,
                CameraEyeOffset = map["CameraEyeOffset"].AsVector3,

                PhysicsShapeType = (PrimitivePhysicsShapeType)map["PhysicsShapeType"].AsInt,
                PathfindingType = (PathfindingType)map["PathfindingType"].AsInt,
                PathfindingCharacterType = (CharacterType)map["PathfindingCharacterType"].AsInt,
                WalkableCoefficientAvatar = map["WalkableCoefficientAvatar"].AsReal,
                WalkableCoefficientA = map["WalkableCoefficientA"].AsReal,
                WalkableCoefficientB = map["WalkableCoefficientB"].AsReal,
                WalkableCoefficientC = map["WalkableCoefficientC"].AsReal,
                WalkableCoefficientD = map["WalkableCoefficientD"].AsReal,
                Material = (PrimitiveMaterial)map["Material"].AsInt,
                Size = map["Size"].AsVector3,

                MediaURL = map["MediaURL"].ToString(),

                AngularVelocity = map["AngularVelocity"].AsVector3,
                Damage = map["Damage"].AsReal,
                ParticleSystemBytes = (BinaryData)map["ParticleSystem"],
                TextureEntryBytes = (BinaryData)map["TextureEntryBytes"],
                TextureAnimationBytes = (BinaryData)map["TextureAnimationBytes"],

                ScriptAccessPin = map["ScriptAccessPin"].AsInt,

                ForceMouselook = map["ForceMouselook"].AsBoolean,

                BaseMask = (InventoryPermissionsMask)map["BasePermissions"].AsUInt,
                OwnerMask = (InventoryPermissionsMask)map["CurrentPermissions"].AsUInt,
                EveryoneMask = (InventoryPermissionsMask)map["EveryOnePermissions"].AsUInt,
                GroupMask = (InventoryPermissionsMask)map["GroupPermissions"].AsUInt,
                NextOwnerMask = (InventoryPermissionsMask)map["NextOwnerPermissions"].AsUInt,

                ClickAction = (ClickActionType)map["ClickAction"].AsInt,
                PointLight = new PointLightParam
                {
                    DbSerialization = (BinaryData)map["LightData"]
                },
                Projection = new ProjectionParam
                {
                    DbSerialization = (BinaryData)map["ProjectionData"]
                },

                ExtendedMesh = new ExtendedMeshParams
                {
                    DbSerialization = (BinaryData)map["ExtendedMeshData"]
                },

                Text = new TextParam
                {
                    Serialization = (BinaryData)map["HoverTextData"]
                },

                Flexible = new FlexibleParam
                {
                    DbSerialization = (BinaryData)map["FlexibleData"]
                },

                Sound = new SoundParam
                {
                    Serialization = (BinaryData)map["LoopedSoundData"]
                },

                CollisionSound = new CollisionSoundParam
                {
                    Serialization = (BinaryData)map["ImpactSoundData"]
                },

                Shape = new ObjectPart.PrimitiveShape
                {
                    Serialization = (BinaryData)map["PrimitiveShapeData"]
                },
                PassCollisionMode = (PassEventMode)map["PassCollisionMode"].AsInt,
                PassTouchMode = (PassEventMode)map["PassTouchMode"].AsInt,
                Velocity = map["Velocity"].AsVector3,
                IsSoundQueueing = map["IsSoundQueueing"].AsBoolean,
                IsAllowedDrop = map["IsAllowedDrop"].AsBoolean,

                PhysicsDensity = map["PhysicsDensity"].AsReal,
                PhysicsFriction = map["PhysicsFriction"].AsReal,
                PhysicsRestitution = map["PhysicsRestitution"].AsReal,
                PhysicsGravityMultiplier = map["PhysicsGravityMultiplier"].AsReal,

                IsRotateXEnabled = map["IsRotateXEnabled"].AsBoolean,
                IsRotateYEnabled = map["IsRotateYEnabled"].AsBoolean,
                IsRotateZEnabled = map["IsRotateZEnabled"].AsBoolean,
                IsVolumeDetect = map["IsVolumeDetect"].AsBoolean,
                IsPhantom = map["IsPhantom"].AsBoolean,
                IsPhysics = map["IsPhysics"].AsBoolean,
                IsSandbox = map["IsSandbox"].AsBoolean,
                IsBlockGrab = map["IsBlockGrab"].AsBoolean,
                IsDieAtEdge = map["IsDieAtEdge"].AsBoolean,
                IsReturnAtEdge = map["IsReturnAtEdge"].AsBoolean,
                IsBlockGrabObject = map["IsBlockGrabObject"].AsBoolean,
                SandboxOrigin = map["SandboxOrigin"].AsVector3,
                IsSitTargetActive = map["IsSitTargetActive"].AsBoolean,
                IsScriptedSitOnly = map["IsScriptedSitOnly"].AsBoolean,
                AllowUnsit = map["AllowUnsit"].AsBoolean,
                IsUnSitTargetActive = map["IsUnSitTargetActive"].AsBoolean,
                UnSitTargetOffset = map["UnSitTargetOffset"].AsVector3,
                UnSitTargetOrientation = map["UnSitTargetOrientation"].AsQuaternion,

                LocalizationSerialization = (BinaryData)map["LocalizationData"]
            };
            objpart.LoadFromVehicleSerialization((BinaryData)map["VehicleData"]);

            using (var ms = new MemoryStream((BinaryData)map["DynAttrs"]))
            {
                foreach (var kvp in (Map)LlsdBinary.Deserialize(ms))
                {
                    objpart.DynAttrs.Add(kvp.Key, kvp.Value);
                }
            }
            return objpart;
        }

        private ObjectPartInventoryItem ObjectPartInventoryItemFromMap(Map map)
        {
            var item = new ObjectPartInventoryItem(map["InventoryID"].AsUUID)
            {
                AssetID = map["AssetID"].AsUUID,
                AssetType = (AssetType)map["AssetType"].AsInt,
                CreationDate = (Date)map["CreationDate"],
                Creator = new UGUI(map["Creator"].ToString()),
                Description = map["Description"].ToString(),
                Flags = (InventoryFlags)map["Flags"].AsUInt,
                Group = new UGI(map["Group"].ToString()),
                IsGroupOwned = map["GroupOwned"].AsBoolean,
                InventoryType = (InventoryType)map["InventoryType"].AsInt,
                LastOwner = new UGUI(map["LastOwner"].ToString()),
                Name = map["Name"].ToString(),
                Owner = new UGUI(map["Owner"].ToString()),
                ParentFolderID = map["ParentFolderID"].AsUUID,
                ExperienceID = map["ExperienceID"].AsUUID,
                CollisionFilter = new ObjectPartInventoryItem.CollisionFilterParam
                {
                    DbSerialization = (BinaryData)map["CollisionFilterData"]
                }
            };
            item.Permissions.Base = (InventoryPermissionsMask)map["BasePermissions"].AsUInt;
            item.Permissions.Current = (InventoryPermissionsMask)map["CurrentPermissions"].AsUInt;
            item.Permissions.EveryOne = (InventoryPermissionsMask)map["EveryOnePermissions"].AsUInt;
            item.Permissions.Group = (InventoryPermissionsMask)map["GroupPermissions"].AsUInt;
            item.Permissions.NextOwner = (InventoryPermissionsMask)map["NextOwnerPermissions"].AsUInt;
            item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType)map["SaleType"].AsUInt;
            item.SaleInfo.Price = map["SalePrice"].AsInt;
            item.SaleInfo.PermMask = (InventoryPermissionsMask)map["SalePermMask"].AsInt;
            item.NextOwnerAssetID = map["NextOwnerAssetID"].AsUUID;
            var grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
            if ((map["PermsGranter"].ToString()).Length != 0)
            {
                try
                {
                    grantinfo.PermsGranter = new UGUI(map["PermsGranter"].ToString());
                }
                catch
                {
                    /* no action required */
                }
            }
            grantinfo.PermsMask = (ScriptPermissions)map["PermsMask"].AsUInt;
            grantinfo.DebitPermissionKey = map["DebitPermissionKey"].AsUUID;

            return item;
        }
        #endregion

        #region Load all object groups of a single region
        List<ObjectGroup> ISimulationDataObjectStorageInterface.LoadObjects(UUID regionID, bool skipErrors)
        {
            var objGroups = new Dictionary<UUID, ObjectGroup>();
            var originalAssetIDs = new Dictionary<UUID, UUID>();
            var nextOwnerAssetIDs = new Dictionary<UUID, UUID>();
            var objGroupParts = new Dictionary<UUID, SortedDictionary<int, ObjectPart>>();
            var objPartIDs = new List<UUID>();
            var objParts = new Dictionary<UUID, ObjectPart>();
            var orphanedPrims = new List<UUID>();
            var orphanedPrimInventories = new List<KeyValuePair<UUID, UUID>>();

            var objectList = new RwLockedDictionary<UUID, Map>();
            var primitiveList = new RwLockedDictionary<UUID, Map>();
            var primItemList = new RwLockedDictionary<string, Map>();

            if (m_Objects.TryGetValue(regionID, out objectList))
            {
                UUID objgroupID = UUID.Zero;
                m_Log.InfoFormat("Loading object groups for region ID {0}", regionID);

                foreach (KeyValuePair<UUID, Map> kvp in objectList)
                {
                    try
                    {
                        objgroupID = kvp.Key;
                        originalAssetIDs[objgroupID] = kvp.Value["OriginalAssetID"].AsUUID;
                        nextOwnerAssetIDs[objgroupID] = kvp.Value["NextOwnerAssetID"].AsUUID;
                        objGroups[objgroupID] = ObjectGroupFromMap(kvp.Value);
                    }
                    catch (Exception e)
                    {
                        m_Log.WarnFormat("Failed to load object {0}: {1}\n{2}", objgroupID, e.Message, e.StackTrace);
                        objGroups.Remove(objgroupID);
                    }
                }

                m_Log.InfoFormat("Loading prims for region ID {0}", regionID);
                int primcount = 0;
                if (m_Primitives.TryGetValue(regionID, out primitiveList))
                {
                    foreach (KeyValuePair<UUID, Map> kvp in primitiveList)
                    {
                        UUID rootPartID = kvp.Value["RootPartID"].AsUUID;
                        if (objGroups.ContainsKey(rootPartID))
                        {
                            if (!objGroupParts.ContainsKey(rootPartID))
                            {
                                objGroupParts.Add(rootPartID, new SortedDictionary<int, ObjectPart>());
                            }

                            var objpart = ObjectPartFromMap(kvp.Value);

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
                if (m_PrimItems.TryGetValue(regionID, out primItemList))
                {
                    foreach (var map in primItemList.Values)
                    {
                        var partID = map["PrimID"].AsUUID;
                        ObjectPart part;
                        if (objParts.TryGetValue(partID, out part))
                        {
                            var item = ObjectPartInventoryItemFromMap(map);

                            if (skipErrors)
                            {
                                try
                                {
                                    part.Inventory.Add(item.ID, item.Name, item);
                                }
                                catch
                                {
                                    m_Log.WarnFormat("deleting duplicate prim in region ID {0}: {1}", regionID, map["ID"].AsUUID);
                                    orphanedPrimInventories.Add(new KeyValuePair<UUID, UUID>(map["PrimID"].AsUUID, map["ID"].AsUUID));
                                }
                            }
                            else
                            {
                                part.Inventory.Add(item.ID, item.Name, item);
                            }
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

            var removeObjGroups = new List<UUID>();
            foreach (var kvp in objGroups)
            {
                if (!objGroupParts.ContainsKey(kvp.Key))
                {
                    removeObjGroups.Add(kvp.Key);
                }
                else
                {
                    foreach (var objpart in objGroupParts[kvp.Key].Values)
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

            foreach (var objid in removeObjGroups)
            {
                objGroups.Remove(objid);
                objectList.Remove(objid);
            }

            foreach (var primid in orphanedPrims)
            {
                primitiveList.Remove(primid);
            }

            foreach (var kvp in orphanedPrimInventories)
            {
                primItemList.Remove(GenItemKey(kvp.Key, kvp.Value));
            }

            return new List<ObjectGroup>(objGroups.Values);
        }
        #endregion
    }
}
