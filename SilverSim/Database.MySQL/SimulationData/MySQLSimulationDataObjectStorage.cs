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
                using(MySqlCommand cmd = new MySqlCommand("SELECT ID FROM objects WHERE RegionID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", key);
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
                using (MySqlCommand cmd = new MySqlCommand("SELECT prims.ID FROM objects INNER JOIN prims ON objects.ID LIKE prims.RootPartID WHERE RegionID LIKE ?id ORDER BY LinkNumber", connection))
                {
                    cmd.Parameters.AddParameter("?id", key);
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

                    using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM objects WHERE RegionID LIKE ?regionid", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        cmd.Parameters.AddParameter("?regionid", regionID);
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT prims.* FROM objects INNER JOIN prims ON objects.id LIKE prims.RootPartID WHERE objects.regionID LIKE ?regionid", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        cmd.Parameters.AddParameter("?regionid", regionID);
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
            using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM primitems WHERE PrimID LIKE ?id", connection))
            {
                cmd.Parameters.AddParameter("?id", objpart.ID);
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM objects WHERE RegionID LIKE ?regionid AND ID LIKE ?id", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        cmd.Parameters.AddParameter("?regionid", regionID);
                        cmd.Parameters.AddParameter("?id", key);
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM prims WHERE RootPartID LIKE ?id ORDER BY LinkNumber", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        cmd.Parameters.AddParameter("?id", key);
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE PrimID LIKE ?primid AND InventoryID LIKE ?itemid", connection))
                {
                    cmd.Parameters.AddParameter("?primid", primID);
                    cmd.Parameters.AddParameter("?itemid", itemID);
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE PrimID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", obj);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM prims WHERE ID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", obj);
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM objects WHERE ID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", obj);
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

        const string UpdateObjectPartInventoryItemSql = "REPLACE INTO primitems " +
            "(AssetId, AssetType, CreationDate, Creator, Description, Flags, `Group`, GroupOwned, PrimID, `Name`, InventoryID, " +
            "InventoryType, LastOwner, Owner, ParentFolderID, BasePermissions, CurrentPermissions, EveryOnePermissions, " + 
            "GroupPermissions, NextOwnerPermissions, SaleType, SalePrice, SalePermMask, PermsGranter, PermsMask)" +
            "VALUES (?AssetId, ?AssetType, ?CreationDate, ?Creator, ?Description, ?Flags, ?Group, ?GroupOwned, ?PrimID, ?Name, ?InventoryID, " +
            "?InventoryType, ?LastOwner, ?Owner, ?ParentFolderID, ?BasePermissions, ?CurrentPermissions, ?EveryOnePermissions, " +
            "?GroupPermissions, ?NextOwnerPermissions, ?SaleType, ?SalePrice, ?SalePermMask, ?PermsGranter, ?PermsMask)";
        private void UpdateObjectPartInventoryItem(MySqlConnection connection, UUID primID, ObjectPartInventoryItem item)
        {
            using (MySqlCommand cmd = new MySqlCommand(UpdateObjectPartInventoryItemSql, connection))
            {
                cmd.Parameters.AddParameter("?AssetId", item.AssetID);
                cmd.Parameters.AddParameter("?AssetType", item.AssetType);
                cmd.Parameters.AddParameter("?CreationDate", item.CreationDate);
                cmd.Parameters.AddParameter("?Creator", item.Creator);
                cmd.Parameters.AddParameter("?Description", item.Description);
                cmd.Parameters.AddParameter("?Flags", item.Flags);
                cmd.Parameters.AddParameter("?Group", item.Group);
                cmd.Parameters.AddParameter("?GroupOwned", item.IsGroupOwned);
                cmd.Parameters.AddParameter("?PrimID", primID);
                cmd.Parameters.AddParameter("?Name", item.Name);
                cmd.Parameters.AddParameter("?InventoryID", item.ID);
                cmd.Parameters.AddParameter("?InventoryType", item.InventoryType);
                cmd.Parameters.AddParameter("?LastOwner", item.LastOwner);
                cmd.Parameters.AddParameter("?Owner", item.Owner);
                cmd.Parameters.AddParameter("?ParentFolderID", item.ParentFolderID);
                cmd.Parameters.AddParameter("?BasePermissions", item.Permissions.Base);
                cmd.Parameters.AddParameter("?CurrentPermissions", item.Permissions.Current);
                cmd.Parameters.AddParameter("?EveryOnePermissions", item.Permissions.EveryOne);
                cmd.Parameters.AddParameter("?GroupPermissions", item.Permissions.Group);
                cmd.Parameters.AddParameter("?NextOwnerPermissions", item.Permissions.NextOwner);
                cmd.Parameters.AddParameter("?SaleType", item.SaleInfo.Type);
                cmd.Parameters.AddParameter("?SalePrice", item.SaleInfo.Price);
                cmd.Parameters.AddParameter("?SalePermMask", item.SaleInfo.PermMask);
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = item.PermsGranter;
                cmd.Parameters.AddParameter("?PermsGranter", grantinfo.PermsGranter.ToString());
                cmd.Parameters.AddParameter("?PermsMask", grantinfo.PermsMask);
                if (cmd.ExecuteNonQuery() < 1)
                {
                    throw new MySQLUtilities.MySQLInsertException();
                }
            }
        }

        const string UpdateObjectGroupSql = "REPLACE INTO objects " + 
            "(ID, RegionID, IsVolumeDetect, IsPhantom, IsPhysics, IsTempOnRez, Owner, LastOwner, `Group`, OriginalAssetID, NextOwnerAssetID, SaleType, SalePrice, PayPrice0, PayPrice1, PayPrice2, PayPrice3, PayPrice4, AttachedPosX, AttachedPosY, AttachedPosZ, AttachPoint)" + 
            "VALUES (?ID, ?RegionID, ?IsVolumeDetect, ?IsPhantom, ?IsPhysics, ?IsTempOnRez, ?Owner, ?LastOwner, ?Group, ?OriginalAssetID, ?NextOwnerAssetID, ?SaleType, ?SalePrice, ?PayPrice0, ?PayPrice1, ?PayPrice2, ?PayPrice3, ?PayPrice4, ?AttachedPosX, ?AttachedPosY, ?AttachedPosZ, ?AttachPoint)";
        private void UpdateObjectGroup(MySqlConnection connection, ObjectGroup objgroup)
        {
            if(objgroup.IsTemporary)
            {
                return;
            }
            using(MySqlCommand cmd = new MySqlCommand(UpdateObjectGroupSql, connection))
            {
                cmd.Parameters.AddParameter("?ID", objgroup.ID);
                cmd.Parameters.AddParameter("?RegionID", objgroup.Scene.ID);
                cmd.Parameters.AddParameter("?IsVolumeDetect", objgroup.IsVolumeDetect);
                cmd.Parameters.AddParameter("?IsPhantom", objgroup.IsPhantom);
                cmd.Parameters.AddParameter("?IsPhysics", objgroup.IsPhysics);
                cmd.Parameters.AddParameter("?IsTempOnRez", objgroup.IsTempOnRez);
                cmd.Parameters.AddParameter("?Owner", objgroup.Owner);
                cmd.Parameters.AddParameter("?LastOwner", objgroup.LastOwner);
                cmd.Parameters.AddParameter("?Group", objgroup.Group);
                cmd.Parameters.AddParameter("?OriginalAssetID", objgroup.OriginalAssetID);
                cmd.Parameters.AddParameter("?NextOwnerAssetID", objgroup.NextOwnerAssetID);
                cmd.Parameters.AddParameter("?SaleType", objgroup.SaleType);
                cmd.Parameters.AddParameter("?SalePrice", objgroup.SalePrice);
                cmd.Parameters.AddParameter("?PayPrice0", objgroup.PayPrice0);
                cmd.Parameters.AddParameter("?PayPrice1", objgroup.PayPrice1);
                cmd.Parameters.AddParameter("?PayPrice2", objgroup.PayPrice2);
                cmd.Parameters.AddParameter("?PayPrice3", objgroup.PayPrice3);
                cmd.Parameters.AddParameter("?PayPrice4", objgroup.PayPrice4);
                cmd.Parameters.AddParameter("?AttachedPos", objgroup.AttachedPos);
                cmd.Parameters.AddParameter("?AttachPoint", objgroup.AttachPoint);
                if (cmd.ExecuteNonQuery() < 1)
                {
                    throw new MySQLUtilities.MySQLInsertException();
                }
            }
        }

        const string UpdateObjectPartSql = "REPLACE INTO prims (" +
                "`PhysicsShapeType`,`ID`,`LinkNumber`,`RootPartID`,`PositionX`,`PositionY`,`PositionZ`,`RotationX`,`RotationY`,`RotationZ`,`RotationW`," +
                "`SitText`,`TouchText`,`Name`,`Description`,`SitTargetOffsetX`,`SitTargetOffsetY`,`SitTargetOffsetZ`," +
                "`SitTargetOrientationX`,`SitTargetOrientationY`,`SitTargetOrientationZ`,`SitTargetOrientationW`," +
                "`Material`,`SizeX`,`SizeY`,`SizeZ`,`SliceX`,`SliceY`,`SliceZ`,`MediaURL`,`Creator`,`CreationDate`," +
                "`Flags`,`AngularVelocityX`,`AngularVelocityY`,`AngularVelocityZ`,`LightData`,`HoverTextData`,`FlexibleData`," +
                "`LoopedSoundData`,`ImpactSoundData`,`PrimitiveShapeData`," +
                "`ParticleSystem`,`TextureEntryBytes`,`TextureAnimationBytes`,`ScriptAccessPin`,`DynAttrs`," +
                "CameraAtOffsetX, CameraAtOffsetY, CameraAtOffsetZ, CameraEyeOffsetX, CameraEyeOffsetY, CameraEyeOffsetZ, ForceMouselook" +
                ") VALUES " +
                "(?v_PhysicsShapeType,?v_ID,?v_LinkNumber,?v_RootPartID,?v_PositionX,?v_PositionY,?v_PositionZ,?v_RotationX,?v_RotationY,?v_RotationZ," +
                "?v_RotationW,?v_SitText,?v_TouchText,?v_Name,?v_Description,?v_SitTargetOffsetX,?v_SitTargetOffsetY,?v_SitTargetOffsetZ," +
                "?v_SitTargetOrientationX,?v_SitTargetOrientationY,?v_SitTargetOrientationZ,?v_SitTargetOrientationW," +
                "?v_Material,?v_SizeX,?v_SizeY,?v_SizeZ,?v_SliceX,?v_SliceY,?v_SliceZ,?v_MediaURL,?v_Creator,?v_CreationDate," +
                "?v_Flags,?v_AngularVelocityX,?v_AngularVelocityY,?v_AngularVelocityZ,?v_LightData,?v_HoverTextData,?v_FlexibleData," +
                "?v_LoopedSoundData,?v_ImpactSoundData,?v_PrimitiveShapeData," +
                "?v_ParticleSystem,?v_TextureEntryBytes,?v_TextureAnimationBytes,?v_ScriptAccessPin,?v_DynAttrs, " +
                "?v_CameraAtOffsetX, ?v_CameraAtOffsetY, ?v_CameraAtOffsetZ, " +
                "?v_CameraEyeOffsetX, ?v_CameraEyeOffsetY, ?v_CameraEyeOffsetZ, ?v_ForceMouselook)";
        private void UpdateObjectPart(MySqlConnection connection, ObjectPart objpart)
        {
            if(objpart.ObjectGroup.IsTemporary || objpart.ObjectGroup.IsTempOnRez)
            {
                return;
            }

            using (MySqlCommand cmd = new MySqlCommand(UpdateObjectPartSql, connection))
            {
                cmd.Parameters.AddParameter("?v_ID", objpart.ID);
                cmd.Parameters.AddParameter("?v_LinkNumber", objpart.LinkNumber);
                cmd.Parameters.AddParameter("?v_RootPartID", objpart.ObjectGroup.RootPart.ID);
                cmd.Parameters.AddParameter("?v_Position", objpart.Position);
                cmd.Parameters.AddParameter("?v_Rotation", objpart.Rotation);

                cmd.Parameters.AddParameter("?v_SitText", objpart.SitText);
                cmd.Parameters.AddParameter("?v_TouchText", objpart.TouchText);
                cmd.Parameters.AddParameter("?v_Name", objpart.Name);
                cmd.Parameters.AddParameter("?v_Description", objpart.Description);
                cmd.Parameters.AddParameter("?v_SitTargetOffset", objpart.SitTargetOffset);
                cmd.Parameters.AddParameter("?v_SitTargetOrientation", objpart.SitTargetOrientation);
                cmd.Parameters.AddParameter("?v_PhysicsShapeType", objpart.PhysicsShapeType);
                cmd.Parameters.AddParameter("?v_Material", objpart.Material);
                cmd.Parameters.AddParameter("?v_Size", objpart.Size);
                cmd.Parameters.AddParameter("?v_Slice", objpart.Slice);
                cmd.Parameters.AddParameter("?v_MediaURL", objpart.MediaURL);
                cmd.Parameters.AddParameter("?v_Creator", objpart.Creator);
                cmd.Parameters.AddParameter("?v_CreationDate", objpart.CreationDate);
                cmd.Parameters.AddParameter("?v_Flags", objpart.Flags);
                cmd.Parameters.AddParameter("?v_AngularVelocity", objpart.AngularVelocity);
                cmd.Parameters.AddParameter("?v_LightData", objpart.PointLight.Serialization);
                cmd.Parameters.AddParameter("?v_HoverTextData", objpart.Text.Serialization);
                cmd.Parameters.AddParameter("?v_FlexibleData", objpart.Flexible.Serialization);
                cmd.Parameters.AddParameter("?v_LoopedSoundData", objpart.Sound.Serialization);
                cmd.Parameters.AddParameter("?v_ImpactSoundData", objpart.CollisionSound.Serialization);
                cmd.Parameters.AddParameter("?v_PrimitiveShapeData", objpart.Shape.Serialization);
                cmd.Parameters.AddParameter("?v_ParticleSystem", objpart.ParticleSystemBytes);
                cmd.Parameters.AddParameter("?v_TextureEntryBytes", objpart.TextureEntryBytes);
                cmd.Parameters.AddParameter("?v_TextureAnimationBytes", objpart.TextureAnimationBytes);
                cmd.Parameters.AddParameter("?v_ScriptAccessPin", objpart.ScriptAccessPin);
                cmd.Parameters.AddParameter("?v_CameraAtOffset", objpart.CameraAtOffset);
                cmd.Parameters.AddParameter("?v_CameraEyeOffset", objpart.CameraEyeOffset);
                cmd.Parameters.AddParameter("?v_ForceMouselook", objpart.ForceMouselook);

                using (MemoryStream ms = new MemoryStream())
                { 
                    LlsdBinary.Serialize(objpart.DynAttrs, ms);
                    cmd.Parameters.AddWithValue("?v_DynAttrs", ms.GetBuffer());
                }

                if (cmd.ExecuteNonQuery() < 1)
                {
                    throw new MySQLUtilities.MySQLInsertException();
                }
            }
        }

        #endregion
    }
}
