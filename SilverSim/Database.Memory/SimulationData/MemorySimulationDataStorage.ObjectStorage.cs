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
        private ObjectGroup ObjectGroupFromMap(Map map) => new ObjectGroup()
        {
            IsTempOnRez = map["IsTempOnRez"].AsBoolean,
            Owner = new UUI(map["Owner"].ToString()),
            LastOwner = new UUI(map["LastOwner"].ToString()),
            Group = new UGI(map["Group"].ToString()),
            SaleType = (InventoryItem.SaleInfoData.SaleType)map["SaleType"].AsUInt,
            SalePrice = map["SalePrice"].AsInt,
            PayPrice0 = map["PayPrice0"].AsInt,
            PayPrice1 = map["PayPrice1"].AsInt,
            PayPrice2 = map["PayPrice2"].AsInt,
            PayPrice3 = map["PayPrice3"].AsInt,
            PayPrice4 = map["PayPrice4"].AsInt,
            AttachedPos = map["AttachedPos"].AsVector3,
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
                Creator = new UUI(map["Creator"].ToString()),
                CreationDate = (Date)map["CreationDate"],
                RezDate = (Date)map["RezDate"],
                Flags = (PrimitiveFlags)map["Flags"].AsUInt,

                CameraAtOffset = map["CameraAtOffset"].AsVector3,
                CameraEyeOffset = map["CameraEyeOffset"].AsVector3,

                PhysicsShapeType = (PrimitivePhysicsShapeType)map["PhysicsShapeType"].AsInt,
                PathfindingType = (PathfindingType)map["PathfindingType"].AsInt,
                WalkableCoefficientAvatar = map["WalkableCoefficientAvatar"].AsReal,
                WalkableCoefficientA = map["WalkableCoefficientA"].AsReal,
                WalkableCoefficientB = map["WalkableCoefficientB"].AsReal,
                WalkableCoefficientC = map["WalkableCoefficientC"].AsReal,
                WalkableCoefficientD = map["WalkableCoefficientD"].AsReal,
                Material = (PrimitiveMaterial)map["Material"].AsInt,
                Size = map["Size"].AsVector3,
                Slice = map["Slice"].AsVector3,

                MediaURL = map["MediaURL"].ToString(),

                AngularVelocity = map["AngularVelocity"].AsVector3
            };
            objpart.PointLight = new ObjectPart.PointLightParam
            {
                DbSerialization = (BinaryData)map["LightData"]
            };
            objpart.Projection = new ObjectPart.ProjectionParam
            {
                DbSerialization = (BinaryData)map["ProjectionData"]
            };

            objpart.ExtendedMesh = new ObjectPart.ExtendedMeshParams
            {
                DbSerialization = (BinaryData)map["ExtendedMeshData"]
            };

            objpart.Text = new ObjectPart.TextParam
            {
                Serialization = (BinaryData)map["HoverTextData"]
            };

            objpart.Flexible = new ObjectPart.FlexibleParam
            {
                DbSerialization = (BinaryData)map["FlexibleData"]
            };

            objpart.Sound = new ObjectPart.SoundParam
            {
                Serialization = (BinaryData)map["LoopedSoundData"]
            };

            objpart.CollisionSound = new ObjectPart.CollisionSoundParam
            {
                Serialization = (BinaryData)map["ImpactSoundData"]
            };

            objpart.Shape = new ObjectPart.PrimitiveShape
            {
                Serialization = (BinaryData)map["PrimitiveShapeData"]
            };

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

            using (var ms = new MemoryStream((BinaryData)map["DynAttrs"]))
            {
                foreach (var kvp in (Map)LlsdBinary.Deserialize(ms))
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
            objpart.IsSandbox = map["IsSandbox"].AsBoolean;
            objpart.IsBlockGrab = map["IsBlockGrab"].AsBoolean;
            objpart.IsDieAtEdge = map["IsDieAtEdge"].AsBoolean;
            objpart.IsReturnAtEdge = map["IsReturnAtEdge"].AsBoolean;
            objpart.IsBlockGrabObject = map["IsBlockGrabObject"].AsBoolean;
            objpart.SandboxOrigin = map["SandboxOrigin"].AsVector3;

            return objpart;
        }

        private ObjectPartInventoryItem ObjectPartInventoryItemFromMap(Map map)
        {
            var item = new ObjectPartInventoryItem(map["InventoryID"].AsUUID)
            {
                AssetID = map["AssetID"].AsUUID,
                AssetType = (AssetType)map["AssetType"].AsInt,
                CreationDate = (Date)map["CreationDate"],
                Creator = new UUI(map["Creator"].ToString()),
                Description = map["Description"].ToString(),
                Flags = (InventoryFlags)map["Flags"].AsUInt,
                Group = new UGI(map["Group"].ToString()),
                IsGroupOwned = map["GroupOwned"].AsBoolean,
                InventoryType = (InventoryType)map["InventoryType"].AsInt,
                LastOwner = new UUI(map["LastOwner"].ToString()),
                Name = map["Name"].ToString(),
                Owner = new UUI(map["Owner"].ToString()),
                ParentFolderID = map["ParentFolderID"].AsUUID
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
        List<ObjectGroup> ISimulationDataObjectStorageInterface.this[UUID regionID]
        {
            get
            {
                var objGroups = new Dictionary<UUID, ObjectGroup>();
                var originalAssetIDs = new Dictionary<UUID, UUID>();
                var nextOwnerAssetIDs = new Dictionary<UUID, UUID>();
                var objGroupParts = new Dictionary<UUID, SortedDictionary<int, ObjectPart>>();
                var objPartIDs = new List<UUID>();
                var objParts = new Dictionary<UUID,ObjectPart>();
                var orphanedPrims = new List<UUID>();
                var orphanedPrimInventories = new List<KeyValuePair<UUID, UUID>>();

                var objectList = new RwLockedDictionary<UUID, Map>();
                var primitiveList = new RwLockedDictionary<UUID, Map>();
                var primItemList = new RwLockedDictionary<string, Map>();

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
                    if(m_PrimItems.TryGetValue(regionID, out primItemList))
                    {
                        foreach(var map in primItemList.Values)
                        {
                            var partID = map["PrimID"].AsUUID;
                            ObjectPart part;
                            if (objParts.TryGetValue(partID, out part))
                            {
                                var item = ObjectPartInventoryItemFromMap(map);

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

                var removeObjGroups = new List<UUID>();
                foreach(var kvp in objGroups)
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

                foreach(var objid in removeObjGroups)
                {
                    objGroups.Remove(objid);
                    objectList.Remove(objid);
                }

                foreach(var primid in orphanedPrims)
                {
                    primitiveList.Remove(primid);
                }

                foreach(var kvp in orphanedPrimInventories)
                {
                    primItemList.Remove(GenItemKey(kvp.Key, kvp.Value));
                }

                return new List<ObjectGroup>(objGroups.Values);
            }
        }
        #endregion
    }
}
