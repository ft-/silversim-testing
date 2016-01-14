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
using System.Linq;
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
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM prims WHERE RegionID LIKE '" + key.ToString() + "' ORDER BY ID, LinkNumber", connection))
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
        ObjectGroup ObjectGroupFromDbReader(MySqlDataReader dbReader)
        {
            ObjectGroup objgroup = new ObjectGroup();
            objgroup.IsVolumeDetect = dbReader.GetBool("IsVolumeDetect");
            objgroup.IsPhantom = dbReader.GetBool("IsPhantom");
            objgroup.IsPhysics = dbReader.GetBool("IsPhysics");
            objgroup.IsTempOnRez = dbReader.GetBool("IsTempOnRez");
            objgroup.Owner = dbReader.GetUUI("Owner");
            objgroup.LastOwner = dbReader.GetUUI("LastOwner");
            objgroup.Group = dbReader.GetUGI("Group");
            objgroup.SaleType = dbReader.GetEnum<InventoryItem.SaleInfoData.SaleType>("SaleType");
            objgroup.SalePrice = dbReader.GetInt32("SalePrice");
            objgroup.PayPrice0 = dbReader.GetInt32("PayPrice0");
            objgroup.PayPrice1 = dbReader.GetInt32("PayPrice1");
            objgroup.PayPrice2 = dbReader.GetInt32("PayPrice2");
            objgroup.PayPrice3 = dbReader.GetInt32("PayPrice3");
            objgroup.PayPrice4 = dbReader.GetInt32("PayPrice4");
            objgroup.AttachedPos = dbReader.GetVector3("AttachedPos");
            objgroup.AttachPoint = dbReader.GetEnum<AttachmentPoint>("AttachPoint");
            return objgroup;
        }

        ObjectPart ObjectPartFromDbReader(MySqlDataReader dbReader)
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

        ObjectPartInventoryItem ObjectPartInventoryItemFromDbReader(MySqlDataReader dbReader)
        {
            ObjectPartInventoryItem item = new ObjectPartInventoryItem();
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

            return item;
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
                                    originalAssetIDs[objgroupID] = dbReader.GetUUID("OriginalAssetID");
                                    nextOwnerAssetIDs[objgroupID] = dbReader.GetUUID("NextOwnerAssetID");
                                    ObjectGroup objgroup = ObjectGroupFromDbReader(dbReader); ;
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM prims WHERE RegionID LIKE '" + regionID.ToString() + "'", connection))
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

                                    ObjectPart objpart = ObjectPartFromDbReader(dbReader);

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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM primitems WHERE RegionID LIKE '" + regionID.ToString() + "'", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                UUID partID = dbReader.GetUUID("RootPartID");
                                ObjectPart part;
                                if (objParts.TryGetValue(partID, out part))
                                {
                                    ObjectPartInventoryItem item = ObjectPartInventoryItemFromDbReader(dbReader);

                                    part.Inventory.Add(item.ID, item.Name, item);
                                    if ((++primitemcount) % 5000 == 0)
                                    {
                                        m_Log.InfoFormat("Loading prim inventories for region ID {0} - {1} loaded", regionID, primitemcount);
                                    }
                                }
                                else
                                {
                                    m_Log.WarnFormat("deleting orphan prim in region ID {0}: {1}", regionID, dbReader.GetUUID("ID"));
                                    orphanedPrimInventories.Add(new KeyValuePair<UUID, UUID>(dbReader.GetUUID("PrimID"), dbReader.GetUUID("ID")));
                                }
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
                        objGroups.Remove(kvp.Key);
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

                for(int idx = 0; idx < removeObjGroups.Count; idx += 256)
                {
                    int elemcnt = Math.Min(removeObjGroups.Count - idx, 256);
                    string sqlcmd = "DELETE FROM objects WHERE RegionID LIKE '" + regionID.ToString() + "' AND ID IN (" +
                        string.Join(",", from id in removeObjGroups.GetRange(idx, elemcnt) select "'" + id.ToString() + "'") +
                        ")";
                    using (MySqlConnection conn = new MySqlConnection())
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(sqlcmd, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                for(int idx = 0; idx < orphanedPrims.Count; idx += 256)
                {
                    int elemcnt = Math.Min(removeObjGroups.Count - idx, 256);
                    string sqlcmd = "DELETE FROM prims WHERE RegionID LIKE '" + regionID.ToString() + "' ID IN (" +
                        string.Join(",", from id in orphanedPrims.GetRange(idx, elemcnt) select "'" + id.ToString() + "'") +
                        ")";
                    using (MySqlConnection conn = new MySqlConnection())
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(sqlcmd, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                for(int idx = 0; idx < orphanedPrimInventories.Count; idx += 256)
                {
                    int elemcnt = Math.Min(orphanedPrimInventories.Count - idx, 256);
                    string sqlcmd = "DELETE FROM primitems WHERE RegionID LIKE '" + regionID.ToString() + "' AND (" +
                        string.Join(" OR ", from id in orphanedPrimInventories.GetRange(idx, elemcnt) select 
                                            string.Format("PrimID LIKE '{0}' AND ID LIKE '{1}'", id.Key.ToString(), id.Value.ToString()));
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
        
        #region Storage Functions
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
        

        #endregion
    }
}
