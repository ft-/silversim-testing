// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Database;
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

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataObjectStorage : SimulationDataObjectStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");

        readonly string m_ConnectionString;
        public MySQLSimulationDataObjectStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        #region Objects and Prims within a region by UUID
        public override List<UUID> ObjectsInRegion(UUID key)
        {
            List<UUID> objects = new List<UUID>();

            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using(MySqlCommand cmd = new MySqlCommand("SELECT ID FROM objects WHERE RegionID LIKE '" + key.ToString() + "'", connection))
                {
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            objects.Add(dbReader.GetUUID("ID"));
                        }
                    }
                }
            }
            return objects;
        }

        public override List<UUID> PrimitivesInRegion(UUID key)
        {
            List<UUID> objects = new List<UUID>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT prims.ID FROM objects INNER JOIN prims ON objects.ID LIKE prims.RootPartID WHERE RegionID LIKE '" + key.ToString() + "' ORDER BY LinkNumber", connection))
                {
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            objects.Add(dbReader.GetUUID("ID"));
                        }
                    }
                }
            }
            return objects;
        }
        #endregion

        #region helpers
        ObjectPart FromDbReader(MySqlDataReader dbReader)
        {
            ObjectPart objpart = new ObjectPart();
            objpart.ID = dbReader.GetUUID("ID");
            objpart.LoadedLinkNumber = dbReader.GetInt32("LinkNumber");
            objpart.Position = dbReader.GetVector3("Position");
            objpart.Rotation = dbReader.GetQuaternion("Rotation");
            objpart.SitText = dbReader.GetString("SitText");
            objpart.TouchText = dbReader.GetString("TouchText");
            objpart.Name = dbReader.GetString("Name");
            objpart.Description = dbReader.GetString("Description");
            objpart.SitTargetOffset = dbReader.GetVector3("SitTargetOffset");
            objpart.SitTargetOrientation = dbReader.GetQuaternion("SitTargetOrientation");
            objpart.Creator = dbReader.GetUUI("Creator");
            objpart.CreationDate = dbReader.GetDate("CreationDate");
            objpart.Flags = dbReader.GetEnum<PrimitiveFlags>("Flags");

            objpart.CameraAtOffset = dbReader.GetVector3("CameraAtOffset");
            objpart.CameraEyeOffset = dbReader.GetVector3("CameraEyeOffset");

            objpart.PhysicsShapeType = dbReader.GetEnum<PrimitivePhysicsShapeType>("PhysicsShapeType");
            objpart.Material = dbReader.GetEnum<PrimitiveMaterial>("Material");
            objpart.Size = dbReader.GetVector3("Size");
            objpart.Slice = dbReader.GetVector3("Slice");

            objpart.MediaURL = dbReader.GetString("MediaURL");

            objpart.AngularVelocity = dbReader.GetVector3("AngularVelocity");

            ObjectPart.PointLightParam lp = new ObjectPart.PointLightParam();
            lp.Serialization = dbReader.GetBytes("LightData");
            objpart.PointLight = lp;

            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Serialization = dbReader.GetBytes("HoverTextData");
            objpart.Text = tp;

            ObjectPart.FlexibleParam fp = new ObjectPart.FlexibleParam();
            fp.Serialization = dbReader.GetBytes("FlexibleData");
            objpart.Flexible = fp;

            ObjectPart.SoundParam sound = new ObjectPart.SoundParam();
            sound.Serialization = dbReader.GetBytes("LoopedSoundData");
            objpart.Sound = sound;

            ObjectPart.CollisionSoundParam collisionsound = new ObjectPart.CollisionSoundParam();
            collisionsound.Serialization = dbReader.GetBytes("ImpactSoundData");
            objpart.CollisionSound = collisionsound;

            ObjectPart.PrimitiveShape ps = new ObjectPart.PrimitiveShape();
            ps.Serialization = dbReader.GetBytes("PrimitiveShapeData");
            objpart.Shape = ps;

            objpart.ParticleSystemBytes = dbReader.GetBytes("ParticleSystem");
            objpart.TextureEntryBytes = dbReader.GetBytes("TextureEntryBytes");
            objpart.TextureAnimationBytes = dbReader.GetBytes("TextureAnimationBytes");

            objpart.ScriptAccessPin = dbReader.GetInt32("ScriptAccessPin");
            objpart.LoadedLinkNumber = dbReader.GetInt32("LinkNumber");

            objpart.ForceMouselook = dbReader.GetBoolean("ForceMouselook");

            objpart.BaseMask = dbReader.GetEnum<InventoryPermissionsMask>("BasePermissions");
            objpart.OwnerMask = dbReader.GetEnum<InventoryPermissionsMask>("CurrentPermissions");
            objpart.EveryoneMask = dbReader.GetEnum<InventoryPermissionsMask>("EveryOnePermissions");
            objpart.GroupMask = dbReader.GetEnum<InventoryPermissionsMask>("GroupPermissions");
            objpart.NextOwnerMask = dbReader.GetEnum<InventoryPermissionsMask>("NextOwnerPermissions");

            using (MemoryStream ms = new MemoryStream(dbReader.GetBytes("DynAttrs")))
            {
                foreach (KeyValuePair<string, IValue> kvp in (Map)LlsdBinary.Deserialize(ms))
                {
                    objpart.DynAttrs.Add(kvp.Key, kvp.Value);
                }
            }
            return objpart;
        }
        #endregion

        #region Load all object groups of a single region
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override List<ObjectGroup> this[UUID regionID]
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

                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    UUID objgroupID = UUID.Zero;
                    m_Log.InfoFormat("Loading object groups for region ID {0}", regionID);

                    using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM objects WHERE RegionID LIKE '" + regionID.ToString() + "'", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        using(MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while(dbReader.Read())
                            {
                                try
                                {

                                    objgroupID = MySQLUtilities.GetUUID(dbReader, "id");
                                    ObjectGroup objgroup = new ObjectGroup();
                                    objgroup.IsVolumeDetect = dbReader.GetBool("IsVolumeDetect");
                                    objgroup.IsPhantom = dbReader.GetBool("IsPhantom");
                                    objgroup.IsPhysics = dbReader.GetBool("IsPhysics");
                                    objgroup.IsTempOnRez = dbReader.GetBool("IsTempOnRez");
                                    objgroup.Owner = dbReader.GetUUI("Owner");
                                    objgroup.LastOwner = dbReader.GetUUI("LastOwner");
                                    objgroup.Group = dbReader.GetUGI("Group");
                                    originalAssetIDs[objgroupID] = dbReader.GetUUID("OriginalAssetID");
                                    nextOwnerAssetIDs[objgroupID] = dbReader.GetUUID("NextOwnerAssetID");
                                    objgroup.SaleType = dbReader.GetEnum<InventoryItem.SaleInfoData.SaleType>("SaleType");
                                    objgroup.SalePrice = dbReader.GetInt32("SalePrice");
                                    objgroup.PayPrice0 = dbReader.GetInt32("PayPrice0");
                                    objgroup.PayPrice1 = dbReader.GetInt32("PayPrice1");
                                    objgroup.PayPrice2 = dbReader.GetInt32("PayPrice2");
                                    objgroup.PayPrice3 = dbReader.GetInt32("PayPrice3");
                                    objgroup.PayPrice4 = dbReader.GetInt32("PayPrice4");
                                    objgroup.AttachedPos = dbReader.GetVector3("AttachedPos");
                                    objgroup.AttachPoint = dbReader.GetEnum<AttachmentPoint>("AttachPoint");
                                    objGroups[objgroupID] = objgroup;
                                }
                                catch(Exception e)
                                {
                                    m_Log.WarnFormat("Failed to load object {0}: {1}\n{2}", objgroupID, e.Message, e.StackTrace);
                                    objGroups.Remove(objgroupID);
                                }
                            }
                        }
                    }

                    m_Log.InfoFormat("Loading prims for region ID {0}", regionID);
                    int primcount = 0;
                    using (MySqlCommand cmd = new MySqlCommand("SELECT prims.* FROM objects INNER JOIN prims ON objects.id LIKE prims.RootPartID WHERE objects.regionID LIKE '" + regionID.ToString() + "'", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                UUID rootPartID = dbReader.GetUUID("RootPartID");
                                if (objGroups.ContainsKey(rootPartID))
                                {
                                    if(!objGroupParts.ContainsKey(rootPartID))
                                    {
                                        objGroupParts.Add(rootPartID, new SortedDictionary<int, ObjectPart>());
                                    }

                                    ObjectPart objpart = FromDbReader(dbReader);

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
                                    m_Log.WarnFormat("deleting orphan prim in region ID {0}: {1}", regionID, dbReader.GetUUID("ID"));
                                    orphanedPrims.Add(dbReader.GetUUID("ID"));
                                }
                            }
                        }
                    }
                    m_Log.InfoFormat("Loaded prims for region ID {0} - {1} loaded", regionID, primcount);

                    int primitemcount = 0;
                    m_Log.InfoFormat("Loading prim inventories for region ID {0}", regionID);
                    foreach(ObjectPart part in objParts.Values)
                    {
                        LoadInventory(connection, part);
                        if ((++primitemcount) % 5000 == 0)
                        {
                            m_Log.InfoFormat("Loading prim inventories for region ID {0} - {1} loaded", regionID, primitemcount);
                        }
                    }
                    m_Log.InfoFormat("Loaded prim inventories for region ID {0} - {1} loaded", regionID, primitemcount);
                }

                List<UUID> removeObjGroups = new List<UUID>();
                foreach(KeyValuePair<UUID, ObjectGroup> kvp in objGroups)
                {
                    if (!objGroupParts.ContainsKey(kvp.Key))
                    {
                        DeleteObjectGroup(kvp.Key);
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
                            DeleteObjectGroup(kvp.Key);
                            removeObjGroups.Add(kvp.Key);
                        }
                    }
                }

                foreach (UUID key in removeObjGroups)
                {
                    objGroups.Remove(key);
                }

                foreach(UUID orphanedPrim in orphanedPrims)
                {
                    DeleteObjectPart(orphanedPrim);
                }

                foreach(KeyValuePair<UUID, UUID> kvp in orphanedPrimInventories)
                {
                    DeleteObjectPartPrimItem(kvp.Key, kvp.Value);
                }

                foreach (KeyValuePair<UUID, ObjectGroup> kvp in objGroups)
                {
                    foreach (ObjectPart opart in kvp.Value.Values)
                    {
                        opart.SerialNumberLoadedFromDatabase = opart.SerialNumber;
                    }
                }

                return new List<ObjectGroup>(objGroups.Values);
            }
        }
        #endregion

        #region Load single object group
        private void LoadInventory(ObjectPart objpart)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                LoadInventory(connection, objpart);
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void LoadInventory(MySqlConnection connection, ObjectPart objpart)
        {
            using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM primitems WHERE PrimID LIKE '" + objpart.ID.ToString() + "'", connection))
            {
                using (MySqlDataReader dbReader = cmd.ExecuteReader())
                {
                    ObjectPartInventoryItem item;

                    while (dbReader.Read())
                    {
                        item = new ObjectPartInventoryItem();
                        item.AssetID = dbReader.GetUUID("AssetID");
                        item.AssetType = dbReader.GetEnum<AssetType>("AssetType");
                        item.CreationDate = dbReader.GetDate("CreationDate");
                        item.Creator = dbReader.GetUUI("Creator");
                        item.Description = dbReader.GetString("Description");
                        item.Flags = dbReader.GetEnum<InventoryFlags>("Flags");
                        item.Group = dbReader.GetUGI("Group");
                        item.IsGroupOwned = dbReader.GetBool("GroupOwned");
                        item.ID = dbReader.GetUUID("InventoryID");
                        item.InventoryType = dbReader.GetEnum<InventoryType>("InventoryType");
                        item.LastOwner = dbReader.GetUUI("LastOwner");
                        item.Name = dbReader.GetString("Name");
                        item.Owner = dbReader.GetUUI("Owner");
                        item.ParentFolderID = dbReader.GetUUID("ParentFolderID");
                        item.Permissions.Base = dbReader.GetEnum<InventoryPermissionsMask>("BasePermissions");
                        item.Permissions.Current = dbReader.GetEnum<InventoryPermissionsMask>("CurrentPermissions");
                        item.Permissions.EveryOne = dbReader.GetEnum<InventoryPermissionsMask>("EveryOnePermissions");
                        item.Permissions.Group = dbReader.GetEnum<InventoryPermissionsMask>("GroupPermissions");
                        item.Permissions.NextOwner = dbReader.GetEnum<InventoryPermissionsMask>("NextOwnerPermissions");
                        item.SaleInfo.Type = dbReader.GetEnum<InventoryItem.SaleInfoData.SaleType>("SaleType");
                        item.SaleInfo.Price = dbReader.GetInt32("SalePrice");
                        item.SaleInfo.PermMask = dbReader.GetEnum<InventoryPermissionsMask>("SalePermMask");
                        ObjectPartInventoryItem.PermsGranterInfo grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
                        if (((string)dbReader["PermsGranter"]).Length != 0)
                        {
                            try
                            {
                                grantinfo.PermsGranter = dbReader.GetUUI("PermsGranter");
                            }
                            catch
                            {
                                /* no action required */
                            }
                        }
                        grantinfo.PermsMask = dbReader.GetEnum<ScriptPermissions>("PermsMask");

                        objpart.Inventory.Add(item.ID, item.Name, item);
                    }
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override ObjectGroup this[UUID regionID, UUID key]
        {
            get
            {
                ObjectGroup objgroup;
                using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    UUID originalAssetID;
                    UUID nextOwnerAssetID;
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM objects WHERE RegionID LIKE '" + regionID.ToString() + "' AND ID LIKE '" + key.ToString() + "'", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (!dbReader.Read())
                            {
                                throw new InvalidOperationException();
                            }

                            objgroup = new ObjectGroup();
                            objgroup.IsVolumeDetect = dbReader.GetBool("IsVolumeDetect");
                            objgroup.IsPhantom = dbReader.GetBool("IsPhantom");
                            objgroup.IsPhysics = dbReader.GetBool("IsPhysics");
                            objgroup.IsTempOnRez = dbReader.GetBool("IsTempOnRez");
                            objgroup.Owner = dbReader.GetUUI("Owner");
                            objgroup.LastOwner = dbReader.GetUUI("LastOwner");
                            objgroup.Group = dbReader.GetUGI("Group");
                            originalAssetID = dbReader.GetUUID("OriginalAssetID");
                            nextOwnerAssetID = dbReader.GetUUID("NextOwnerAssetID");
                            objgroup.SaleType = dbReader.GetEnum<InventoryItem.SaleInfoData.SaleType>("SaleType");
                            objgroup.SalePrice = dbReader.GetInt32("SalePrice");
                            objgroup.PayPrice0 = dbReader.GetInt32("PayPrice0");
                            objgroup.PayPrice1 = dbReader.GetInt32("PayPrice1");
                            objgroup.PayPrice2 = dbReader.GetInt32("PayPrice2");
                            objgroup.PayPrice3 = dbReader.GetInt32("PayPrice3");
                            objgroup.PayPrice4 = dbReader.GetInt32("PayPrice4");
                            objgroup.AttachedPos = dbReader.GetVector3("AttachedPos");
                            objgroup.AttachPoint = dbReader.GetEnum<AttachmentPoint>("AttachPoint");
                        }
                    }
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM prims WHERE RootPartID LIKE '" + key.ToString() + "' ORDER BY LinkNumber", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        using(MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while(dbReader.Read())
                            {
                                ObjectPart objpart = FromDbReader(dbReader);
                                LoadInventory(objpart);
                                objgroup.Add(dbReader.GetInt32("LinkNumber"), objpart.ID, objpart);
                            }
                        }
                    }
                    objgroup.OriginalAssetID = originalAssetID;
                    objgroup.NextOwnerAssetID = nextOwnerAssetID;
                }
                objgroup.FinalizeObject();
                foreach (ObjectPart opart in objgroup.Values)
                {
                    opart.SerialNumberLoadedFromDatabase = opart.SerialNumber;
                }
                return objgroup;
            }
        }
        #endregion

        #region Delete functions
        void DeleteObjectPartPrimItem(UUID primID, UUID itemID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE PrimID LIKE '" +  
                    primID.ToString() + "' AND InventoryID LIKE '" + itemID.ToString() + "'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void DeleteObjectPart(UUID obj)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                DeleteObjectPart(connection, obj);
            }
        }

        public void DeleteObjectPart(MySqlConnection connection, UUID obj)
        {
            connection.InsideTransaction(delegate()
            {
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE PrimID LIKE '" + obj.ToString() + "'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM prims WHERE ID LIKE '" + obj.ToString() + "'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            });
        }

        public override void DeleteObjectGroup(UUID obj)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                DeleteObjectGroup(connection, obj);
            }
        }

        public void DeleteObjectGroup(MySqlConnection connection, UUID obj)
        {
            connection.InsideTransaction(delegate()
            {
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM objects WHERE ID LIKE '" + obj.ToString() + "'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            });
        }
        #endregion

        #region Storage Functions
        public override void UpdateObjectPart(ObjectPart objpart)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                ObjectGroup grp = objpart.ObjectGroup;
                connection.Open();
                if (null != grp && objpart.LinkNumber == 1)
                {
                    UpdateObjectGroup(connection, grp);
                }
                UpdateObjectPart(connection, objpart);
                foreach (ObjectPartInventoryItem item in objpart.Inventory.Values)
                {
                    UpdateObjectPartInventoryItem(connection, objpart.ID, item);
                }
            }
        }

        public void UpdateObjectPartInner(MySqlConnection connection, ObjectPart objpart)
        {
            ObjectGroup grp = objpart.ObjectGroup;
            if (null != grp && objpart.LinkNumber == 1)
            {
                UpdateObjectGroup(connection, grp);
            }
            UpdateObjectPart(connection, objpart);
            foreach (ObjectPartInventoryItem item in objpart.Inventory.Values)
            {
                UpdateObjectPartInventoryItem(connection, objpart.ID, item);
            }
        }

        private void UpdateObjectPartInventoryItem(MySqlConnection connection, UUID primID, ObjectPartInventoryItem item)
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
            connection.ReplaceInto("primitems", data);
        }

        private void UpdateObjectGroup(MySqlConnection connection, ObjectGroup objgroup)
        {
            if(objgroup.IsTemporary)
            {
                return;
            }
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
            connection.ReplaceInto("objects", data);
        }

        private void UpdateObjectPart(MySqlConnection connection, ObjectPart objpart)
        {
            if(objpart.ObjectGroup.IsTemporary || objpart.ObjectGroup.IsTempOnRez)
            {
                return;
            }

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

            connection.ReplaceInto("prims", data);
        }

        #endregion
    }
}
