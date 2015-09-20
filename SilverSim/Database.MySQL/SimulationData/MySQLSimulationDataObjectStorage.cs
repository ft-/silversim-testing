// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataObjectStorage : SimulationDataObjectStorageInterface, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");

        public string m_ConnectionString;
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
                    cmd.Parameters.AddWithValue("?id", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            objects.Add(new UUID(dbReader["ID"].ToString()));
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
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM prims WHERE RegionID LIKE ?id ORDER BY LinkNumber", connection))
                {
                    cmd.Parameters.AddWithValue("?id", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            objects.Add(new UUID(dbReader["ID"].ToString()));
                        }
                    }
                }
            }
            return objects;
        }
        #endregion

        #region Load all object groups of a single region
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
                        cmd.Parameters.AddWithValue("?regionid", regionID);
                        using(MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while(dbReader.Read())
                            {
                                try
                                {

                                    objgroupID = MySQLUtilities.GetUUID(dbReader, "id");
                                    ObjectGroup objgroup = new ObjectGroup();
                                    objgroup.IsVolumeDetect = MySQLUtilities.GetBoolean(dbReader, "IsVolumeDetect");
                                    objgroup.IsPhantom = MySQLUtilities.GetBoolean(dbReader, "IsPhantom");
                                    objgroup.IsPhysics = MySQLUtilities.GetBoolean(dbReader, "IsPhysics");
                                    objgroup.IsTempOnRez = MySQLUtilities.GetBoolean(dbReader, "IsTempOnRez");
                                    objgroup.Owner = dbReader.GetUUI("Owner");
                                    objgroup.LastOwner = new UUI((string)dbReader["LastOwner"]);
                                    objgroup.Group = dbReader.GetUGI("Group");
                                    originalAssetIDs[objgroupID] = dbReader.GetUUID("OriginalAssetID");
                                    nextOwnerAssetIDs[objgroupID] = dbReader.GetUUID("NextOwnerAssetID");
                                    objgroup.SaleType = (InventoryItem.SaleInfoData.SaleType)(int)dbReader["SaleType"];
                                    objgroup.SalePrice = (int)dbReader["SalePrice"];
                                    objgroup.PayPrice0 = (int)dbReader["PayPrice0"];
                                    objgroup.PayPrice1 = (int)dbReader["PayPrice1"];
                                    objgroup.PayPrice2 = (int)dbReader["PayPrice2"];
                                    objgroup.PayPrice3 = (int)dbReader["PayPrice3"];
                                    objgroup.PayPrice4 = (int)dbReader["PayPrice4"];
                                    objgroup.AttachedPos = dbReader.GetVector("AttachedPos");
                                    objgroup.AttachPoint = (AttachmentPoint)(uint)dbReader["AttachPoint"];
                                    objGroups[objgroupID] = objgroup;
                                }
                                catch(Exception e)
                                {
                                    m_Log.WarnFormat("Failed to load object {0}: {1}\n{2}", objgroupID, e.Message, e.StackTrace.ToString());
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
                        cmd.Parameters.AddWithValue("?regionid", regionID);
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
                                    ObjectPart objpart = new ObjectPart();
                                    objpart.ID = dbReader.GetUUID("ID");
                                    objpart.LoadedLinkNumber = (int)dbReader["LinkNumber"];
                                    objpart.Position = MySQLUtilities.GetVector(dbReader, "Position");
                                    objpart.Rotation = MySQLUtilities.GetQuaternion(dbReader, "Rotation");
                                    objpart.SitText = (string)dbReader["SitText"];
                                    objpart.TouchText = (string)dbReader["TouchText"];
                                    objpart.Name = (string)dbReader["Name"];
                                    objpart.Description = (string)dbReader["Description"];
                                    objpart.SitTargetOffset = MySQLUtilities.GetVector(dbReader, "SitTargetOffset");
                                    objpart.SitTargetOrientation = MySQLUtilities.GetQuaternion(dbReader, "SitTargetOrientation");
                                    objpart.Creator = dbReader.GetUUI("Creator");
                                    objpart.CreationDate = MySQLUtilities.GetDate(dbReader, "CreationDate");
                                    objpart.Flags = (PrimitiveFlags)(uint)dbReader["Flags"];

                                    objpart.PhysicsShapeType = (PrimitivePhysicsShapeType)(int)dbReader["PhysicsShapeType"];
                                    objpart.Material = (PrimitiveMaterial)(int)dbReader["Material"];
                                    objpart.Size = MySQLUtilities.GetVector(dbReader, "Size");
                                    objpart.Slice = MySQLUtilities.GetVector(dbReader, "Slice");

                                    objpart.MediaURL = (string)dbReader["MediaURL"];

                                    objpart.AngularVelocity = MySQLUtilities.GetVector(dbReader, "AngularVelocity");

                                    ObjectPart.PointLightParam lp = new ObjectPart.PointLightParam();
                                    lp.IsLight = MySQLUtilities.GetBoolean(dbReader, "LightEnabled");
                                    lp.LightColor = MySQLUtilities.GetColor(dbReader, "LightColor");
                                    lp.Intensity = (double)dbReader["LightIntensity"];
                                    lp.Radius = (double)dbReader["LightRadius"];
                                    lp.Falloff = (double)dbReader["LightFalloff"];
                                    objpart.PointLight = lp;

                                    ObjectPart.TextParam tp = new ObjectPart.TextParam();
                                    tp.Text = (string)dbReader["HoverText"];
                                    tp.TextColor = MySQLUtilities.GetColorAlpha(dbReader, "HoverTextColor");
                                    objpart.Text = tp;

                                    ObjectPart.FlexibleParam fp = new ObjectPart.FlexibleParam();
                                    fp.IsFlexible = MySQLUtilities.GetBoolean(dbReader, "IsFlexible");
                                    fp.Friction = (double)dbReader["FlexibleFriction"];
                                    fp.Gravity = (double)dbReader["FlexibleGravity"];
                                    fp.Softness = (int)dbReader["FlexibleSoftness"];
                                    fp.Wind = (double)dbReader["FlexibleWind"];
                                    fp.Force = MySQLUtilities.GetVector(dbReader, "FlexibleForce");
                                    objpart.Flexible = fp;

                                    ObjectPart.SoundParam sound = new ObjectPart.SoundParam();
                                    sound.SoundID = dbReader.GetUUID("LoopedSound");
                                    sound.Radius = (double)dbReader["SoundRadius"];
                                    sound.Gain = (double)dbReader["SoundGain"];
                                    sound.Flags = (PrimitiveSoundFlags)(uint)dbReader["SoundFlags"];
                                    objpart.Sound = sound;

                                    ObjectPart.CollisionSoundParam collisionsound = new ObjectPart.CollisionSoundParam();
                                    collisionsound.ImpactSound = dbReader.GetUUID("ImpactSound");
                                    collisionsound.ImpactVolume = (double)dbReader["ImpactVolume"];
                                    objpart.CollisionSound = collisionsound;

                                    ObjectPart.PrimitiveShape ps = new ObjectPart.PrimitiveShape();

                                    ps.PathBegin = (ushort)(uint)dbReader["PathBegin"];
                                    ps.PathCurve = (byte)(uint)dbReader["PathCurve"];
                                    ps.PathEnd = (ushort)(uint)dbReader["PathEnd"];
                                    ps.PathRadiusOffset = (sbyte)(int)dbReader["PathRadiusOffset"];
                                    ps.PathRevolutions = (byte)(uint)dbReader["PathRevolutions"];
                                    ps.PathScaleX = (byte)(uint)dbReader["PathScaleX"];
                                    ps.PathScaleY = (byte)(uint)dbReader["PathScaleY"];
                                    ps.PathShearX = (byte)(uint)dbReader["PathShearX"];
                                    ps.PathShearY = (byte)(uint)dbReader["PathShearY"];
                                    ps.PathSkew = (sbyte)(int)dbReader["PathSkew"];
                                    ps.PathTaperX = (sbyte)(int)dbReader["PathTaperX"];
                                    ps.PathTaperY = (sbyte)(int)dbReader["PathTaperY"];
                                    ps.PathTwist = (sbyte)(int)dbReader["PathTwist"];
                                    ps.PathTwistBegin = (sbyte)(int)dbReader["PathTwistBegin"];
                                    ps.ProfileBegin = (ushort)(uint)dbReader["ProfileBegin"];
                                    ps.ProfileCurve = (byte)(uint)dbReader["ProfileCurve"];
                                    ps.ProfileEnd = (ushort)(uint)dbReader["ProfileEnd"];
                                    ps.ProfileHollow = (ushort)(uint)dbReader["ProfileHollow"];
                                    ps.IsSculptInverted = dbReader.GetBoolean("IsShapeSculptInverted");
                                    ps.IsSculptMirrored = dbReader.GetBoolean("IsShapeSculptMirrored");
                                    ps.SculptMap = dbReader.GetUUID("ShapeSculptMap");
                                    ps.SculptType = (PrimitiveSculptType)(int)dbReader["ShapeSculptType"];
                                    ps.Type = (PrimitiveShapeType)(int)dbReader["ShapeType"];
                                    ps.PCode = (PrimitiveCode)(uint)dbReader["PCode"];
                                    objpart.Shape = ps;

                                    objpart.ParticleSystemBytes = dbReader.GetBytes("ParticleSystem");
                                    objpart.ExtraParamsBytes = dbReader.GetBytes("ExtraParams");
                                    objpart.TextureEntryBytes = dbReader.GetBytes("TextureEntryBytes");
                                    objpart.TextureAnimationBytes = dbReader.GetBytes("TextureAnimationBytes");

                                    objpart.ScriptAccessPin = (int)dbReader["ScriptAccessPin"];
                                    objpart.LoadedLinkNumber = (int)dbReader["LinkNumber"];

                                    using (MemoryStream ms = new MemoryStream((byte[])dbReader["DynAttrs"]))
                                    {
                                        foreach (KeyValuePair<string, IValue> kvp in (Map)LLSD_Binary.Deserialize(ms))
                                        {
                                            objpart.DynAttrs.Add(kvp.Key, kvp.Value);
                                        }
                                    }

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

        private void LoadInventory(MySqlConnection connection, ObjectPart objpart)
        {
            using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM primitems WHERE PrimID LIKE ?id", connection))
            {
                cmd.Parameters.AddWithValue("?id", objpart.ID);
                using (MySqlDataReader dbReader = cmd.ExecuteReader())
                {
                    ObjectPartInventoryItem item;

                    while (dbReader.Read())
                    {
                        item = new ObjectPartInventoryItem();
                        item.AssetID = dbReader.GetUUID("AssetID");
                        item.AssetType = (AssetType)(int)dbReader["AssetType"];
                        item.CreationDate = MySQLUtilities.GetDate(dbReader, "CreationDate");
                        item.Creator = dbReader.GetUUI("Creator");
                        item.Description = (string)dbReader["Description"];
                        item.Flags = (uint)dbReader["Flags"];
                        item.Group = dbReader.GetUGI("Group");
                        item.IsGroupOwned = (uint)dbReader["GroupOwned"] != 0;
                        item.ID = dbReader.GetUUID("InventoryID");
                        item.InventoryType = (InventoryType)(int)dbReader["InventoryType"];
                        item.LastOwner = new UUI((string)dbReader["LastOwner"]);
                        item.Name = (string)dbReader["Name"];
                        item.Owner = dbReader.GetUUI("Owner");
                        item.ParentFolderID = dbReader.GetUUID("ParentFolderID");
                        item.Permissions.Base = (InventoryPermissionsMask)(uint)dbReader["BasePermissions"];
                        item.Permissions.Current = (InventoryPermissionsMask)(uint)dbReader["CurrentPermissions"];
                        item.Permissions.EveryOne = (InventoryPermissionsMask)(uint)dbReader["EveryOnePermissions"];
                        item.Permissions.Group = (InventoryPermissionsMask)(uint)dbReader["GroupPermissions"];
                        item.Permissions.NextOwner = (InventoryPermissionsMask)(uint)dbReader["NextOwnerPermissions"];
                        item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType)(int)dbReader["SaleType"];
                        item.SaleInfo.Price = (int)dbReader["SalePrice"];
                        item.SaleInfo.PermMask = (InventoryPermissionsMask)(uint)dbReader["SalePermMask"];
                        ObjectPartInventoryItem.PermsGranterInfo grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
                        if ((string)dbReader["PermsGranter"] != "")
                        {
                            try
                            {
                                grantinfo.PermsGranter = dbReader.GetUUI("PermsGranter");
                            }
                            catch
                            {

                            }
                        }
                        grantinfo.PermsMask = (Types.Script.ScriptPermissions)(uint)dbReader["PermsMask"];

                        objpart.Inventory.Add(item.ID, item.Name, item);
                    }
                }
            }
        }

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
                        cmd.Parameters.AddWithValue("?regionid", regionID);
                        cmd.Parameters.AddWithValue("?id", key);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (!dbReader.Read())
                            {
                                throw new InvalidOperationException();
                            }

                            objgroup = new ObjectGroup();
                            objgroup.IsVolumeDetect = MySQLUtilities.GetBoolean(dbReader, "IsVolumeDetect");
                            objgroup.IsPhantom = MySQLUtilities.GetBoolean(dbReader, "IsPhantom");
                            objgroup.IsPhysics = MySQLUtilities.GetBoolean(dbReader, "IsPhysics");
                            objgroup.IsTempOnRez = MySQLUtilities.GetBoolean(dbReader, "IsTempOnRez");
                            objgroup.Owner = dbReader.GetUUI("Owner");
                            objgroup.LastOwner = new UUI((string)dbReader["LastOwner"]);
                            objgroup.Group = dbReader.GetUGI("Group");
                            originalAssetID = dbReader.GetUUID("OriginalAssetID");
                            nextOwnerAssetID = dbReader.GetUUID("NextOwnerAssetID");
                            objgroup.SaleType = (InventoryItem.SaleInfoData.SaleType)(int)dbReader["SaleType"];
                            objgroup.SalePrice = (int)dbReader["SalePrice"];
                            objgroup.PayPrice0 = (int)dbReader["PayPrice0"];
                            objgroup.PayPrice1 = (int)dbReader["PayPrice1"];
                            objgroup.PayPrice2 = (int)dbReader["PayPrice2"];
                            objgroup.PayPrice3 = (int)dbReader["PayPrice3"];
                            objgroup.PayPrice4 = (int)dbReader["PayPrice4"];
                            objgroup.AttachedPos = dbReader.GetVector("AttachedPos");
                            objgroup.AttachPoint = (AttachmentPoint)(uint)dbReader["AttachPoint"];
                        }
                    }
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM prims WHERE RootPartID LIKE ?id ORDER BY LinkNumber", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        cmd.Parameters.AddWithValue("?id", key);
                        using(MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while(dbReader.Read())
                            {
                                ObjectPart objpart = new ObjectPart();
                                objpart.ID = dbReader.GetUUID("ID");
                                objpart.LoadedLinkNumber = (int)dbReader["LinkNumber"];
                                objpart.Position = MySQLUtilities.GetVector(dbReader, "Position");
                                objpart.Rotation = MySQLUtilities.GetQuaternion(dbReader, "Rotation");
                                objpart.SitText = (string)dbReader["SitText"];
                                objpart.TouchText = (string)dbReader["TouchText"];
                                objpart.Name = (string)dbReader["Name"];
                                objpart.Description = (string)dbReader["Description"];
                                objpart.SitTargetOffset = MySQLUtilities.GetVector(dbReader, "SitTargetOffset");
                                objpart.SitTargetOrientation = MySQLUtilities.GetQuaternion(dbReader, "SitTargetOrientation");
                                objpart.Creator = dbReader.GetUUI("Creator");
                                objpart.CreationDate = MySQLUtilities.GetDate(dbReader, "CreationDate");
                                objpart.Flags = (PrimitiveFlags)(uint)dbReader["Flags"];

                                objpart.PhysicsShapeType = (PrimitivePhysicsShapeType)(int)dbReader["PhysicsShapeType"];
                                objpart.Material = (PrimitiveMaterial)(int)dbReader["Material"];
                                objpart.Size = MySQLUtilities.GetVector(dbReader, "Size");
                                objpart.Slice = MySQLUtilities.GetVector(dbReader, "Slice");

                                objpart.MediaURL = (string)dbReader["MediaURL"];

                                objpart.AngularVelocity = MySQLUtilities.GetVector(dbReader, "AngularVelocity");

                                ObjectPart.PointLightParam lp = new ObjectPart.PointLightParam();
                                lp.IsLight = MySQLUtilities.GetBoolean(dbReader, "LightEnabled");
                                lp.LightColor = MySQLUtilities.GetColor(dbReader, "LightColor");
                                lp.Intensity = (double)dbReader["LightIntensity"];
                                lp.Radius = (double)dbReader["LightRadius"];
                                lp.Falloff = (double)dbReader["LightFalloff"];
                                objpart.PointLight = lp;

                                ObjectPart.TextParam tp = new ObjectPart.TextParam();
                                tp.Text = (string)dbReader["HoverText"];
                                tp.TextColor = MySQLUtilities.GetColorAlpha(dbReader, "HoverTextColor");
                                objpart.Text = tp;

                                ObjectPart.FlexibleParam fp = new ObjectPart.FlexibleParam();
                                fp.IsFlexible = MySQLUtilities.GetBoolean(dbReader, "IsFlexible");
                                fp.Friction = (double)dbReader["FlexibleFriction"];
                                fp.Gravity = (double)dbReader["FlexibleGravity"];
                                fp.Softness = (int)dbReader["FlexibleSoftness"];
                                fp.Wind = (double)dbReader["FlexibleWind"];
                                fp.Force = MySQLUtilities.GetVector(dbReader, "FlexibleForce");
                                objpart.Flexible = fp;

                                ObjectPart.SoundParam sound = new ObjectPart.SoundParam();
                                sound.SoundID = dbReader.GetUUID("LoopedSound");
                                sound.Radius = (double)dbReader["SoundRadius"];
                                sound.Gain = (double)dbReader["SoundGain"];
                                sound.Flags = (PrimitiveSoundFlags)(uint)dbReader["SoundFlags"];
                                objpart.Sound = sound;

                                ObjectPart.CollisionSoundParam collisionsound = new ObjectPart.CollisionSoundParam();
                                collisionsound.ImpactSound = dbReader.GetUUID("ImpactSound");
                                collisionsound.ImpactVolume = (double)dbReader["ImpactVolume"];
                                objpart.CollisionSound = collisionsound;

                                ObjectPart.PrimitiveShape ps = new ObjectPart.PrimitiveShape();

                                ps.PathBegin = (ushort)(uint)dbReader["PathBegin"];
                                ps.PathCurve = (byte)(uint)dbReader["PathCurve"];
                                ps.PathEnd = (ushort)(uint)dbReader["PathEnd"];
                                ps.PathRadiusOffset = (sbyte)(int)dbReader["PathRadiusOffset"];
                                ps.PathRevolutions = (byte)(uint)dbReader["PathRevolutions"];
                                ps.PathScaleX = (byte)(uint)dbReader["PathScaleX"];
                                ps.PathScaleY = (byte)(uint)dbReader["PathScaleY"];
                                ps.PathShearX = (byte)(uint)dbReader["PathShearX"];
                                ps.PathShearY = (byte)(uint)dbReader["PathShearY"];
                                ps.PathSkew = (sbyte)(int)dbReader["PathSkew"];
                                ps.PathTaperX = (sbyte)(int)dbReader["PathTaperX"];
                                ps.PathTaperY = (sbyte)(int)dbReader["PathTaperY"];
                                ps.PathTwist = (sbyte)(int)dbReader["PathTwist"];
                                ps.PathTwistBegin = (sbyte)(int)dbReader["PathTwistBegin"];
                                ps.ProfileBegin = (ushort)(uint)dbReader["ProfileBegin"];
                                ps.ProfileCurve = (byte)(uint)dbReader["ProfileCurve"];
                                ps.ProfileEnd = (ushort)(uint)dbReader["ProfileEnd"];
                                ps.ProfileHollow = (ushort)(uint)dbReader["ProfileHollow"];
                                ps.IsSculptInverted = dbReader.GetBoolean("IsShapeSculptInverted");
                                ps.IsSculptMirrored = dbReader.GetBoolean("IsShapeSculptMirrored");
                                ps.SculptMap = dbReader.GetUUID("ShapeSculptMap");
                                ps.SculptType = (PrimitiveSculptType)(int)dbReader["ShapeSculptType"];
                                ps.Type = (PrimitiveShapeType)(int)dbReader["ShapeType"];
                                ps.PCode = (PrimitiveCode)(uint)dbReader["PCode"];
                                objpart.Shape = ps;

                                objpart.ParticleSystemBytes = dbReader.GetBytes("ParticleSystem");
                                objpart.ExtraParamsBytes = dbReader.GetBytes("ExtraParams");
                                objpart.TextureEntryBytes = dbReader.GetBytes("TextureEntryBytes");
                                objpart.TextureAnimationBytes = dbReader.GetBytes("TextureAnimationBytes");

                                objpart.ScriptAccessPin = (int)dbReader["ScriptAccessPin"];

                                using (MemoryStream ms = new MemoryStream((byte[])dbReader["DynAttrs"]))
                                {
                                    foreach(KeyValuePair<string, IValue> kvp in (Map)LLSD_Binary.Deserialize(ms))
                                    {
                                        objpart.DynAttrs.Add(kvp.Key, kvp.Value);
                                    }
                                }

                                LoadInventory(objpart);
                                objgroup.Add((int)dbReader["LinkNumber"], objpart.ID, objpart);
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
                    cmd.Parameters.AddWithValue("?primid", primID);
                    cmd.Parameters.AddWithValue("?itemid", itemID);
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
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM prims WHERE ID LIKE ?id", connection))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE EXISTS (SELECT null FROM prims WHERE primitems.PrimID LIKE prims.ID AND prims.RootPartID LIKE ?id)", connection))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM prims WHERE RootPartID LIKE ?id", connection))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM objects WHERE ID LIKE ?id", connection))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
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
                cmd.Parameters.AddWithValue("?AssetId", item.AssetID.ToString());
                cmd.Parameters.AddWithValue("?AssetType", (int)item.AssetType);
                cmd.Parameters.AddWithValue("?CreationDate", item.CreationDate.AsULong);
                cmd.Parameters.AddWithValue("?Creator", item.Creator.ToString());
                cmd.Parameters.AddWithValue("?Description", item.Description);
                cmd.Parameters.AddWithValue("?Flags", item.Flags);
                cmd.Parameters.AddWithValue("?Group", item.Group.ToString());
                cmd.Parameters.AddWithValue("?GroupOwned", item.IsGroupOwned ? 1 : 0);
                cmd.Parameters.AddWithValue("?PrimID", primID.ToString());
                cmd.Parameters.AddWithValue("?Name", item.Name);
                cmd.Parameters.AddWithValue("?InventoryID", item.ID.ToString());
                cmd.Parameters.AddWithValue("?InventoryType", (int)item.InventoryType);
                cmd.Parameters.AddWithValue("?LastOwner", item.LastOwner.ToString());
                cmd.Parameters.AddWithValue("?Owner", item.Owner.ToString());
                cmd.Parameters.AddWithValue("?ParentFolderID", item.ParentFolderID.ToString());
                cmd.Parameters.AddWithValue("?BasePermissions", (uint)item.Permissions.Base);
                cmd.Parameters.AddWithValue("?CurrentPermissions", (uint)item.Permissions.Current);
                cmd.Parameters.AddWithValue("?EveryOnePermissions", (uint)item.Permissions.EveryOne);
                cmd.Parameters.AddWithValue("?GroupPermissions", (uint)item.Permissions.Group);
                cmd.Parameters.AddWithValue("?NextOwnerPermissions", (uint)item.Permissions.NextOwner);
                cmd.Parameters.AddWithValue("?SaleType", (uint)item.SaleInfo.Type);
                cmd.Parameters.AddWithValue("?SalePrice", item.SaleInfo.Price);
                cmd.Parameters.AddWithValue("?SalePermMask", (uint)item.SaleInfo.PermMask);
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = item.PermsGranter;
                cmd.Parameters.AddWithValue("?PermsGranter", grantinfo.PermsGranter.ToString());
                cmd.Parameters.AddWithValue("?PermsMask", (uint)grantinfo.PermsMask);
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
                cmd.Parameters.AddWithValue("?ID", objgroup.ID.ToString());
                cmd.Parameters.AddWithValue("?RegionID", objgroup.Scene.ID.ToString());
                cmd.Parameters.AddWithValue("?IsVolumeDetect", objgroup.IsVolumeDetect ? 1 : 0);
                cmd.Parameters.AddWithValue("?IsPhantom", objgroup.IsPhantom ? 1 : 0);
                cmd.Parameters.AddWithValue("?IsPhysics", objgroup.IsPhysics ? 1 : 0);
                cmd.Parameters.AddWithValue("?IsTempOnRez", objgroup.IsTempOnRez ? 1 : 0);
                cmd.Parameters.AddWithValue("?Owner", objgroup.Owner.ToString());
                cmd.Parameters.AddWithValue("?LastOwner", objgroup.LastOwner.ToString());
                cmd.Parameters.AddWithValue("?Group", objgroup.Group.ToString());
                cmd.Parameters.AddWithValue("?OriginalAssetID", objgroup.OriginalAssetID.ToString());
                cmd.Parameters.AddWithValue("?NextOwnerAssetID", objgroup.NextOwnerAssetID.ToString());
                cmd.Parameters.AddWithValue("?SaleType", (int)objgroup.SaleType);
                cmd.Parameters.AddWithValue("?SalePrice", objgroup.SalePrice);
                cmd.Parameters.AddWithValue("?PayPrice0", objgroup.PayPrice0);
                cmd.Parameters.AddWithValue("?PayPrice1", objgroup.PayPrice1);
                cmd.Parameters.AddWithValue("?PayPrice2", objgroup.PayPrice2);
                cmd.Parameters.AddWithValue("?PayPrice3", objgroup.PayPrice3);
                cmd.Parameters.AddWithValue("?PayPrice4", objgroup.PayPrice4);
                cmd.Parameters.AddWithValue("?AttachedPosX", objgroup.AttachedPos.X);
                cmd.Parameters.AddWithValue("?AttachedPosY", objgroup.AttachedPos.Y);
                cmd.Parameters.AddWithValue("?AttachedPosZ", objgroup.AttachedPos.Z);
                cmd.Parameters.AddWithValue("?AttachPoint", (uint)objgroup.AttachPoint);
                if (cmd.ExecuteNonQuery() < 1)
                {
                    throw new MySQLUtilities.MySQLInsertException();
                }
            }
        }

        const string UpdateObjectPartSql = "REPLACE INTO prims (`PhysicsShapeType`,`ID`,`LinkNumber`,`RootPartID`,`PositionX`,`PositionY`,`PositionZ`,`RotationX`,`RotationY`,`RotationZ`,`RotationW`,`SitText`,`TouchText`,`Name`,`Description`,`SitTargetOffsetX`,`SitTargetOffsetY`,`SitTargetOffsetZ`,`SitTargetOrientationX`,`SitTargetOrientationY`,`SitTargetOrientationZ`,`SitTargetOrientationW`,`ShapeType`,`Material`,`SizeX`,`SizeY`,`SizeZ`,`SliceX`,`SliceY`,`SliceZ`,`MediaURL`,`Creator`,`CreationDate`,`Flags`,`AngularVelocityX`,`AngularVelocityY`,`AngularVelocityZ`,`LightEnabled`,`LightColorRed`,`LightColorGreen`,`LightColorBlue`,`LightIntensity`,`LightRadius`,`LightFalloff`,`HoverText`,`HoverTextColorRed`,`HoverTextColorGreen`,`HoverTextColorBlue`,`IsFlexible`,`FlexibleFriction`,`FlexibleGravity`,`FlexibleSoftness`,`FlexibleWind`,`FlexibleForceX`,`FlexibleForceY`,`FlexibleForceZ`,`LoopedSound`,`SoundRadius`,`SoundGain`,`SoundFlags`,`ImpactSound`,`ImpactVolume`,`IsShapeSculptInverted`,`IsShapeSculptMirrored`,`ShapeSculptMap`,`ShapeSculptType`,`PathBegin`,`PathCurve`,`PathEnd`,`PathRadiusOffset`,`PathRevolutions`,`PathScaleX`,`PathScaleY`,`PathShearX`,`PathshearY`,`PathSkew`,`PathTaperX`,`PathTaperY`,`PathTwist`,`PathTwistBegin`,`ProfileBegin`,`ProfileCurve`,`ProfileEnd`,`ProfileHollow`,`PCode`,`ExtraParams`,`ParticleSystem`,`TextureEntryBytes`,`TextureAnimationBytes`,`ScriptAccessPin`,`DynAttrs`) VALUES (?v_PhysicsShapeType,?v_ID,?v_LinkNumber,?v_RootPartID,?v_PositionX,?v_PositionY,?v_PositionZ,?v_RotationX,?v_RotationY,?v_RotationZ,?v_RotationW,?v_SitText,?v_TouchText,?v_Name,?v_Description,?v_SitTargetOffsetX,?v_SitTargetOffsetY,?v_SitTargetOffsetZ,?v_SitTargetOrientationX,?v_SitTargetOrientationY,?v_SitTargetOrientationZ,?v_SitTargetOrientationW,?v_ShapeType,?v_Material,?v_SizeX,?v_SizeY,?v_SizeZ,?v_SliceX,?v_SliceY,?v_SliceZ,?v_MediaURL,?v_Creator,?v_CreationDate,?v_Flags,?v_AngularVelocityX,?v_AngularVelocityY,?v_AngularVelocityZ,?v_LightEnabled,?v_LightColorRed,?v_LightColorGreen,?v_LightColorBlue,?v_LightIntensity,?v_LightRadius,?v_LightFalloff,?v_HoverText,?v_HoverTextColorRed,?v_HoverTextColorGreen,?v_HoverTextColorBlue,?v_IsFlexible,?v_FlexibleFriction,?v_FlexibleGravity,?v_FlexibleSoftness,?v_FlexibleWind,?v_FlexibleForceX,?v_FlexibleForceY,?v_FlexibleForceZ,?v_LoopedSound,?v_SoundRadius,?v_SoundGain,?v_SoundFlags,?v_ImpactSound,?v_ImpactVolume,?v_IsShapeSculptInverted,?v_IsShapeSculptMirrored,?v_ShapeSculptMap,?v_ShapeSculptType,?v_PathBegin,?v_PathCurve,?v_PathEnd,?v_PathRadiusOffset,?v_PathRevolutions,?v_PathScaleX,?v_PathScaleY,?v_PathShearX,?v_PathshearY,?v_PathSkew,?v_PathTaperX,?v_PathTaperY,?v_PathTwist,?v_PathTwistBegin,?v_ProfileBegin,?v_ProfileCurve,?v_ProfileEnd,?v_ProfileHollow,?v_PCode,?v_ExtraParams,?v_ParticleSystem,?v_TextureEntryBytes,?v_TextureAnimationBytes,?v_ScriptAccessPin,?v_DynAttrs)";
        private void UpdateObjectPart(MySqlConnection connection, ObjectPart objpart)
        {
            if(objpart.ObjectGroup.IsTemporary || objpart.ObjectGroup.IsTempOnRez)
            {
                return;
            }

            using (MySqlCommand cmd = new MySqlCommand(UpdateObjectPartSql, connection))
            {
                cmd.Parameters.AddWithValue("?v_ID", objpart.ID.ToString());
                cmd.Parameters.AddWithValue("?v_LinkNumber", objpart.LinkNumber);
                cmd.Parameters.AddWithValue("?v_RootPartID", objpart.ObjectGroup.RootPart.ID.ToString());
                cmd.Parameters.AddWithValue("?v_PositionX", objpart.Position.X);
                cmd.Parameters.AddWithValue("?v_PositionY", objpart.Position.Y);
                cmd.Parameters.AddWithValue("?v_PositionZ", objpart.Position.Z);
                cmd.Parameters.AddWithValue("?v_RotationX", objpart.Rotation.X);
                cmd.Parameters.AddWithValue("?v_RotationY", objpart.Rotation.Y);
                cmd.Parameters.AddWithValue("?v_RotationZ", objpart.Rotation.Z);
                cmd.Parameters.AddWithValue("?v_RotationW", objpart.Rotation.W);
                cmd.Parameters.AddWithValue("?v_SitText", objpart.SitText);
                cmd.Parameters.AddWithValue("?v_TouchText", objpart.TouchText);
                cmd.Parameters.AddWithValue("?v_Name", objpart.Name);
                cmd.Parameters.AddWithValue("?v_Description", objpart.Description);
                cmd.Parameters.AddWithValue("?v_SitTargetOffsetX", objpart.SitTargetOffset.X);
                cmd.Parameters.AddWithValue("?v_SitTargetOffsetY", objpart.SitTargetOffset.Y);
                cmd.Parameters.AddWithValue("?v_SitTargetOffsetZ", objpart.SitTargetOffset.Z);
                cmd.Parameters.AddWithValue("?v_SitTargetOrientationX", objpart.SitTargetOrientation.X);
                cmd.Parameters.AddWithValue("?v_SitTargetOrientationY", objpart.SitTargetOrientation.Y);
                cmd.Parameters.AddWithValue("?v_SitTargetOrientationZ", objpart.SitTargetOrientation.Z);
                cmd.Parameters.AddWithValue("?v_SitTargetOrientationW", objpart.SitTargetOrientation.W);
                cmd.Parameters.AddWithValue("?v_PhysicsShapeType", (int)objpart.PhysicsShapeType);
                cmd.Parameters.AddWithValue("?v_Material", (int)objpart.Material);
                cmd.Parameters.AddWithValue("?v_SizeX", objpart.Size.X);
                cmd.Parameters.AddWithValue("?v_SizeY", objpart.Size.Y);
                cmd.Parameters.AddWithValue("?v_SizeZ", objpart.Size.Z);
                cmd.Parameters.AddWithValue("?v_SliceX", objpart.Slice.X);
                cmd.Parameters.AddWithValue("?v_SliceY", objpart.Slice.Y);
                cmd.Parameters.AddWithValue("?v_SliceZ", objpart.Slice.Z);
                cmd.Parameters.AddWithValue("?v_MediaURL", objpart.MediaURL);
                cmd.Parameters.AddWithValue("?v_Creator", objpart.Creator.ToString());
                cmd.Parameters.AddWithValue("?v_CreationDate", objpart.CreationDate.AsULong);
                cmd.Parameters.AddWithValue("?v_Flags", (uint)objpart.Flags);

                cmd.Parameters.AddWithValue("?v_AngularVelocityX", objpart.AngularVelocity.X);
                cmd.Parameters.AddWithValue("?v_AngularVelocityY", objpart.AngularVelocity.Y);
                cmd.Parameters.AddWithValue("?v_AngularVelocityZ", objpart.AngularVelocity.Z);

                ObjectPart.PointLightParam lp = objpart.PointLight;
                cmd.Parameters.AddWithValue("?v_LightEnabled", lp.IsLight ? 1 : 0);
                cmd.Parameters.AddWithValue("?v_LightColorRed", lp.LightColor.R);
                cmd.Parameters.AddWithValue("?v_LightColorGreen", lp.LightColor.G);
                cmd.Parameters.AddWithValue("?v_LightColorBlue", lp.LightColor.B);
                cmd.Parameters.AddWithValue("?v_LightIntensity", lp.Intensity);
                cmd.Parameters.AddWithValue("?v_LightRadius", lp.Radius);
                cmd.Parameters.AddWithValue("?v_LightFalloff", lp.Falloff);

                ObjectPart.TextParam tp = objpart.Text;
                cmd.Parameters.AddWithValue("?v_HoverText", tp.Text);
                cmd.Parameters.AddWithValue("?v_HoverTextColorRed", tp.TextColor.R);
                cmd.Parameters.AddWithValue("?v_HoverTextColorGreen", tp.TextColor.G);
                cmd.Parameters.AddWithValue("?v_HoverTextColorBlue", tp.TextColor.B);
                cmd.Parameters.AddWithValue("?v_HoverTextColorAlpha", tp.TextColor.A);

                ObjectPart.FlexibleParam fp = objpart.Flexible;
                cmd.Parameters.AddWithValue("?v_IsFlexible", fp.IsFlexible ? 1 : 0);
                cmd.Parameters.AddWithValue("?v_FlexibleFriction", fp.Friction);
                cmd.Parameters.AddWithValue("?v_FlexibleGravity", fp.Gravity);
                cmd.Parameters.AddWithValue("?v_FlexibleSoftness", fp.Softness);
                cmd.Parameters.AddWithValue("?v_FlexibleWind", fp.Wind);
                cmd.Parameters.AddWithValue("?v_FlexibleForceX", fp.Force.X);
                cmd.Parameters.AddWithValue("?v_FlexibleForceY", fp.Force.X);
                cmd.Parameters.AddWithValue("?v_FlexibleForceZ", fp.Force.X);

                ObjectPart.SoundParam sound = objpart.Sound;
                cmd.Parameters.AddWithValue("?v_LoopedSound", sound.SoundID.ToString());
                cmd.Parameters.AddWithValue("?v_SoundRadius", sound.Radius);
                cmd.Parameters.AddWithValue("?v_SoundGain", sound.Gain);
                cmd.Parameters.AddWithValue("?v_SoundFlags", (uint)sound.Flags);

                ObjectPart.CollisionSoundParam collisionsound = objpart.CollisionSound;
                cmd.Parameters.AddWithValue("?v_ImpactSound", collisionsound.ImpactSound.ToString());
                cmd.Parameters.AddWithValue("?v_ImpactVolume", collisionsound.ImpactVolume);

                ObjectPart.PrimitiveShape ps = objpart.Shape;
                cmd.Parameters.AddWithValue("?v_IsShapeSculptInverted", ps.IsSculptInverted ? 1 : 0);
                cmd.Parameters.AddWithValue("?v_IsShapeSculptMirrored", ps.IsSculptMirrored ? 1 : 0);
                cmd.Parameters.AddWithValue("?v_ShapeSculptMap", ps.SculptMap.ToString());
                cmd.Parameters.AddWithValue("?v_ShapeSculptType", (int)ps.SculptType);
                cmd.Parameters.AddWithValue("?v_ShapeType", (int)ps.Type);
                cmd.Parameters.AddWithValue("?v_PathBegin", ps.PathBegin);
                cmd.Parameters.AddWithValue("?v_PathCurve", ps.PathCurve);
                cmd.Parameters.AddWithValue("?v_PathEnd", ps.PathEnd);
                cmd.Parameters.AddWithValue("?v_PathRadiusOffset", ps.PathRadiusOffset);
                cmd.Parameters.AddWithValue("?v_PathRevolutions", ps.PathRevolutions);
                cmd.Parameters.AddWithValue("?v_PathScaleX", ps.PathScaleX);
                cmd.Parameters.AddWithValue("?v_PathScaleY", ps.PathScaleY);
                cmd.Parameters.AddWithValue("?v_PathShearX", ps.PathShearX);
                cmd.Parameters.AddWithValue("?v_PathshearY", ps.PathShearY);
                cmd.Parameters.AddWithValue("?v_PathSkew", ps.PathSkew);
                cmd.Parameters.AddWithValue("?v_PathTaperX", ps.PathTaperX);
                cmd.Parameters.AddWithValue("?v_PathTaperY", ps.PathTaperY);
                cmd.Parameters.AddWithValue("?v_PathTwist", ps.PathTwist);
                cmd.Parameters.AddWithValue("?v_PathTwistBegin", ps.PathTwistBegin);
                cmd.Parameters.AddWithValue("?v_ProfileBegin", ps.ProfileBegin);
                cmd.Parameters.AddWithValue("?v_ProfileCurve", ps.ProfileCurve);
                cmd.Parameters.AddWithValue("?v_ProfileEnd", ps.ProfileEnd);
                cmd.Parameters.AddWithValue("?v_ProfileHollow", ps.ProfileHollow);
                cmd.Parameters.AddWithValue("?v_PCode", (int)ps.PCode);

                cmd.Parameters.AddWithValue("?v_ExtraParams", objpart.ExtraParamsBytes);

                cmd.Parameters.AddWithValue("?v_ParticleSystem", objpart.ParticleSystemBytes);

                cmd.Parameters.AddWithValue("?v_TextureEntryBytes", objpart.TextureEntryBytes);
                cmd.Parameters.AddWithValue("?v_TextureAnimationBytes", objpart.TextureAnimationBytes);

                cmd.Parameters.AddWithValue("?v_ScriptAccessPin", objpart.ScriptAccessPin);

                using(MemoryStream ms = new MemoryStream())
                { 
                    LLSD_Binary.Serialize(objpart.DynAttrs, ms);
                    cmd.Parameters.AddWithValue("?v_DynAttrs", ms.GetBuffer());
                }

                if (cmd.ExecuteNonQuery() < 1)
                {
                    throw new MySQLUtilities.MySQLInsertException();
                }
            }
        }

        #endregion

        #region IDBServiceInterface
        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }
        #endregion

        #region Migrations
        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "objects", ObjectsMigrations, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "prims", PrimsMigrations, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "primitems", PrimItemsMigrations, m_Log);
        }

        private static readonly string[] ObjectsMigrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "IsVolumeDetect TINYINT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "IsPhantom TINYINT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "IsPhysics TINYINT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "IsTempOnRez TINYINT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "`Owner` VARCHAR(255) NOT NULL DEFAULT ''," +
                "LastOwner VARCHAR(255) NOT NULL DEFAULT ''," +
                "`Group` VARCHAR(255) NOT NULL DEFAULT ''," +
                "PRIMARY KEY(ID)" +
            ")",
            "ALTER TABLE %tablename% ADD COLUMN (OriginalAssetID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                        "NextOwnerAssetID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'),",
            "ALTER TABLE %tablename% ADD COLUMN (Category INT(11) NOT NULL DEFAULT '0'," +
                        "SaleType INT(11) NOT NULL DEFAULT '0'," + 
                        "SalePrice INT(11) NOT NULL DEFAULT '0'," +
                        "PayPrice0 INT(11) NOT NULL DEFAULT '0'," +
                        "PayPrice1 INT(11) NOT NULL DEFAULT '0'," + 
                        "PayPrice2 INT(11) NOT NULL DEFAULT '0'," + 
                        "PayPrice3 INT(11) NOT NULL DEFAULT '0'," + 
                        "PayPrice4 INT(11) NOT NULL DEFAULT '0'" + 
                        "),",
            "ALTER TABLE %tablename% ADD COLUMN (" +
                "AttachedPosX REAL NOT NULL DEFAULT '0'," + 
                "AttachedPosY REAL NOT NULL DEFAULT '0'," + 
                "AttachedPosZ REAL NOT NULL DEFAULT '0'," + 
                "AttachPoint INT(11) UNSIGNED NOT NULL DEFAULT '0'" +
                "),",
            "ALTER TABLE %tablename% ADD KEY RegionID (RegionID),"
        };

        private static readonly string[] PrimItemsMigrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "PrimID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "InventoryID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "Name VARCHAR(255) NOT NULL," +
                "Description VARCHAR(255) NOT NULL DEFAULT ''," +
                "Flags INT(11) NOT NULL DEFAULT '0'," +
                "AssetId CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "AssetType INT NOT NULL DEFAULT '0'," +
                "CreationDate BIGINT(20) NOT NULL DEFAULT '0'," +
                "Creator VARCHAR(255)," +
                "`Group` VARCHAR(255)," +
                "GroupOwned INT(1) UNSIGNED NOT NULL," +
                "InventoryType INT(11) NOT NULL DEFAULT '0'," +
                "LastOwner VARCHAR(255) NOT NULL DEFAULT ''," +
                "`Owner` VARCHAR(255) NOT NULL," +
                "ParentFolderID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "BasePermissions INT(11) UNSIGNED NOT NULL DEFAULT '0'," + 
                "CurrentPermissions INT(11) UNSIGNED NOT NULL DEFAULT '0'," + 
                "EveryOnePermissions INT(11) UNSIGNED NOT NULL DEFAULT '0'," + 
                "GroupPermissions INT(11) UNSIGNED NOT NULL DEFAULT '0'," + 
                "NextOwnerPermissions INT(11) UNSIGNED NOT NULL DEFAULT '0'," + 
                "SaleType INT(11) NOT NULL DEFAULT '0'," +
                "SalePrice INT(11) NOT NULL DEFAULT '0'," +
                "SalePermMask INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(PrimID, InventoryID)," +
                "KEY primID (PrimID))",
            "ALTER TABLE %tablename% ADD COLUMN (PermsGranter VARCHAR(255) NOT NULL DEFAULT ''," +
                        "PermsMask INT(11) UNSIGNED NOT NULL DEFAULT '0'),",
            "ALTER TABLE %tablename% MODIFY COLUMN Flags INT(11) UNSIGNED NOT NULL DEFAULT '0',"
        };

        private static readonly string[] PrimsMigrations = new string[] {
            "CREATE TABLE %tablename% (" +
                "ID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "RootPartID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "LinkNumber INT(11) NOT NULL," + 
                "Flags INT(11) UNSIGNED NOT NULL," +
                "PositionX REAL NOT NULL," +
                "PositionY REAL NOT NULL," +
                "PositionZ REAL NOT NULL," +
                "RotationX REAL NOT NULL," +
                "RotationY REAL NOT NULL," +
                "RotationZ REAL NOT NULL," +
                "RotationW REAL NOT NULL," +
                "SitText TEXT," +
                "TouchText TEXT," +
                "Creator VARCHAR(255) NOT NULL DEFAULT ''," +
                "CreationDate BIGINT(20) NOT NULL DEFAULT '0'," +
                "Name VARCHAR(64) NOT NULL DEFAULT ''," +
                "Description VARCHAR(255) NOT NULL DEFAULT ''," +
                "DynAttrs BLOB," +
                "SitTargetOffsetX DOUBLE NOT NULL DEFAULT '0'," +
                "SitTargetOffsetY DOUBLE NOT NULL DEFAULT '0'," +
                "SitTargetOffsetZ DOUBLE NOT NULL DEFAULT '0'," +
                "SitTargetOrientationX DOUBLE NOT NULL DEFAULT '0'," +
                "SitTargetOrientationY DOUBLE NOT NULL DEFAULT '0'," +
                "SitTargetOrientationZ DOUBLE NOT NULL DEFAULT '0'," +
                "SitTargetOrientationW DOUBLE NOT NULL DEFAULT '1'," +
                "PhysicsShapeType INT(11) NOT NULL DEFAULT '0'," +
                "Material INT(11) NOT NULL DEFAULT '0'," +
                "SizeX REAL NOT NULL DEFAULT '0'," +
                "SizeY REAL NOT NULL DEFAULT '0'," +
                "SizeZ REAL NOT NULL DEFAULT '0'," +
                "SliceX REAL NOT NULL DEFAULT '0'," +
                "SliceY REAL NOT NULL DEFAULT '0'," +
                "SliceZ REAL NOT NULL DEFAULT '0'," +
                "MediaURL VARCHAR(255) NOT NULL DEFAULT ''," +
                "AngularVelocityX REAL NOT NULL DEFAULT '0'," +
                "AngularVelocityY REAL NOT NULL DEFAULT '0'," +
                "AngularVelocityZ REAL NOT NULL DEFAULT '0'," +
                "LightEnabled INT(1) NOT NULL DEFAULT '0'," +
                "LightColorRed REAL NOT NULL DEFAULT '0'," +
                "LightColorGreen REAL NOT NULL DEFAULT '0'," +
                "LightColorBlue REAL NOT NULL DEFAULT '0'," +
                "LightIntensity REAL NOT NULL DEFAULT '0'," +
                "LightRadius REAL NOT NULL DEFAULT '0'," +
                "LightFalloff REAL NOT NULL DEFAULT '0'," +
                "HoverText TEXT," +
                "HoverTextColorRed REAL NOT NULL DEFAULT '0'," +
                "HoverTextColorGreen REAL NOT NULL DEFAULT '0'," +
                "HoverTextColorBlue REAL NOT NULL DEFAULT '0'," +
                "HoverTextColorAlpha REAL NOT NULL DEFAULT '0'," +
                "IsFlexible INT(1) NOT NULL DEFAULT '0'," +
                "FlexibleForceX REAL NOT NULL DEFAULT '0'," +
                "FlexibleForceY REAL NOT NULL DEFAULT '0'," +
                "FlexibleForceZ REAL NOT NULL DEFAULT '0'," +
                "FlexibleFriction REAL NOT NULL DEFAULT '0'," +
                "FlexibleGravity REAL NOT NULL DEFAULT '0'," +
                "FlexibleSoftness INT(11) NOT NULL DEFAULT '0'," +
                "FlexibleWind REAL NOT NULL DEFAULT '0'," +
                "LoopedSound CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "SoundRadius REAL NOT NULL DEFAULT '0'," +
                "SoundGain REAL NOT NULL DEFAULT '0'," +
                "SoundFlags INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "ImpactSound CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ImpactVolume REAL NOT NULL DEFAULT '0'," +
                "PathBegin INT(11) UNSIGNED NOT NULL," +
                "PathCurve INT(11) UNSIGNED NOT NULL," +
                "PathEnd INT(11) UNSIGNED NOT NULL," +
                "PathRadiusOffset INT(11) NOT NULL," +
                "PathRevolutions INT(11) UNSIGNED NOT NULL," + 
                "PathScaleX INT(11) UNSIGNED NOT NULL," +
                "PathScaleY INT(11) UNSIGNED NOT NULL," +
                "PathShearX INT(11) UNSIGNED NOT NULL," +
                "PathShearY INT(11) UNSIGNED NOT NULL," +
                "PathSkew INT(11) NOT NULL," +
                "PathTaperX INT(11) NOT NULL," +
                "PathTaperY INT(11) NOT NULL," +
                "PathTwist INT(11) NOT NULL," +
                "PathTwistBegin INT(11) NOT NULL," +
                "ProfileBegin INT(11) UNSIGNED NOT NULL," +
                "ProfileCurve INT(11) UNSIGNED NOT NULL," +
                "ProfileEnd INT(11) UNSIGNED NOT NULL," +
                "ProfileHollow INT(11) UNSIGNED NOT NULL," +
                "IsShapeSculptInverted INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "IsShapeSculptMirrored INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "ShapeSculptMap CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ShapeSculptType INT(11) NOT NULL DEFAULT '0'," +
                "ShapeType INT(11) NOT NULL DEFAULT '0'," +
                "PCode INT(11) UNSIGNED NOT NULL," + 
                "ParticleSystem BLOB," +
                "ExtraParams BLOB," +
                "ScriptAccessPin INT(11) NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(ID, RootPartID))",
            "ALTER TABLE %tablename% ADD COLUMN (TextureEntryBytes BLOB, TextureAnimationBytes BLOB),",
            "ALTER TABLE %tablename% ADD KEY RootPartID (RootPartID), ADD UNIQUE KEY ID (ID),",
            "ALTER TABLE %tablename% ADD KEY LinkNumber (LinkNumber),",
        };
        #endregion
    }
}
