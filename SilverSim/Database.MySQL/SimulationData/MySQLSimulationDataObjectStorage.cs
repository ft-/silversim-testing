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

        private void LoadInventory(ObjectPart objpart)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM primitems WHERE PrimID LIKE ?id", connection))
                {
                    cmd.Parameters.AddWithValue("?id", objpart.ID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        ObjectPartInventoryItem item;

                        while (dbReader.Read())
                        {
                            item = new ObjectPartInventoryItem();
                            item.AssetID = (string)dbReader["AssetID"];
                            item.AssetType = (AssetType)(int)dbReader["AssetType"];
                            item.CreationDate = MySQLUtilities.GetDate(dbReader, "CreationDate");
                            item.Creator = new UUI((string)dbReader["Creator"]);
                            item.Description = (string)dbReader["Description"];
                            item.Flags = (uint)dbReader["Flags"];
                            item.Group = new UGI((string)dbReader["Group"]);
                            item.IsGroupOwned = (int)dbReader["GroupOwned"] != 0;
                            item.ID = (string)dbReader["InventoryID"];
                            item.InventoryType = (InventoryType)(int)dbReader["InventoryType"];
                            item.LastOwner = new UUI((string)dbReader["LastOwner"]);
                            item.Name = (string)dbReader["Name"];
                            item.Owner = new UUI((string)dbReader["Owner"]);
                            item.ParentFolderID = (string)dbReader["ParentFolderID"];
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
                                    grantinfo.PermsGranter = new UUI((string)dbReader["PermsGranter"]);
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

        public override void UpdateObjectGroup(ObjectGroup objgroup)
        {
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                UpdateObjectGroup(connection, objgroup);
            }
        }

        public override void UpdateObjectPart(ObjectPart objpart)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                UpdateObjectPart(connection, objpart);
            }
        }

        public override void UpdateObjectPartInventory(ObjectPart objpart)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                foreach (ObjectPartInventoryItem item in objpart.Inventory.Values)
                {
                    UpdateObjectPartInventoryItem(connection, item);
                }
            }
        }

        public override void DeleteObjectPart(UUID obj)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
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
        }

        public override void DeleteObjectGroup(UUID obj)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
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
        }

        #region Storage Functions
        private void UpdateObjectPartInventoryItem(MySqlConnection connection, ObjectPartInventoryItem item)
        {
            Dictionary<string, object> p = new Dictionary<string, object>();
            p["AssetID"] = item.AssetID;
            p["AssetType"] = (int)item.AssetType;
            p["CreationDate"] = item.CreationDate;
            p["Creator"] = item.Creator;
            p["Description"] = item.Description;
            p["Flags"] = item.Flags;
            p["Group"] = item.Group;
            p["GroupOwned"] = item.IsGroupOwned;
            p["ID"] = item.ID;
            p["InventoryType"] = (int)item.InventoryType;
            p["LastOwner"] = item.LastOwner;
            p["ParentFolderID"] = item.ParentFolderID;
            p["BasePermissions"] = item.Permissions.Base;
            p["CurrentPermissions"] = item.Permissions.Current;
            p["EveryOnePermissions"] = item.Permissions.EveryOne;
            p["GroupPermissions"] = item.Permissions.Group;
            p["NextOwnerPermissions"] = item.Permissions.NextOwner;
            p["SaleType"] = item.SaleInfo.Type;
            p["SalePrice"] = item.SaleInfo.Price;
            p["SalePermMask"] = item.SaleInfo.PermMask;
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = item.PermsGranter;
            p["PermsGranter"] = grantinfo.PermsGranter.ToString();
            p["PermsMask"] = (uint)grantinfo.PermsMask;

            MySQLUtilities.ReplaceInsertInto(connection, "primitems", p);
        }

        private void UpdateObjectGroup(MySqlConnection connection, ObjectGroup objgroup)
        {
            if(objgroup.IsTemporary)
            {
                return;
            }
            Dictionary<string, object> p = new Dictionary<string, object>();
            p["ID"] = objgroup.ID;
            p["RegionID"] = objgroup.Scene.ID;
            p["IsVolumeDetect"] = objgroup.IsVolumeDetect ? 1 : 0;
            p["IsPhantom"] = objgroup.IsPhantom ? 1 : 0;
            p["IsPhysics"] = objgroup.IsPhysics ? 1 : 0;
            p["IsTempOnRez"] = objgroup.IsTempOnRez ? 1 : 0;
            p["Owner"] = objgroup.Owner.ToString();
            p["LastOwner"] = objgroup.LastOwner.ToString();
            p["Group"] = objgroup.Group.ToString();
            p["OriginalAssetID"] = objgroup.OriginalAssetID.ToString();
            p["NextOwnerAssetID"] = objgroup.NextOwnerAssetID.ToString();
            p["SaleType"] = (int)objgroup.SaleType;
            p["SalePrice"] = objgroup.SalePrice;
            p["PayPrice0"] = objgroup.PayPrice0;
            p["PayPrice1"] = objgroup.PayPrice1;
            p["PayPrice2"] = objgroup.PayPrice2;
            p["PayPrice3"] = objgroup.PayPrice3;
            p["PayPrice4"] = objgroup.PayPrice4;
            p["AttachedPos"] = objgroup.AttachedPos;
            p["AttachPoint"] = (uint)objgroup.AttachPoint;

            MySQLUtilities.ReplaceInsertInto(connection, "objects", p);
        }

        private void UpdateObjectPart(MySqlConnection connection, ObjectPart objpart)
        {
            if(objpart.ObjectGroup.IsTemporary || objpart.ObjectGroup.IsTempOnRez)
            {
                return;
            }
            Dictionary<string, object> p = new Dictionary<string, object>();
            p["ID"] = objpart.ID;
            p["LinkNumber"] = objpart.LinkNumber;
            p["RootPartID"] = objpart.ObjectGroup.RootPart.ID;
            p["Position"] = objpart.Position;
            p["Rotation"] = objpart.Rotation;
            p["SitText"] = objpart.SitText;
            p["TouchText"] = objpart.TouchText;
            p["Name"] = objpart.Name;
            p["Description"] = objpart.Description;
            p["SitTargetOffset"] = objpart.SitTargetOffset;
            p["SitTargetOrientation"] = objpart.SitTargetOrientation;
            p["ShapeType"] = (int)objpart.PhysicsShapeType;
            p["Material"] = (int)objpart.Material;
            p["Size"] = objpart.Size;
            p["Slice"] = objpart.Slice;
            p["MediaURL"] = objpart.MediaURL;
            p["Creator"] = objpart.Creator.ToString();
            p["CreationDate"] = objpart.CreationDate.AsULong;
            p["Flags"] = (uint)objpart.Flags;

            p["AngularVelocity"] = objpart.AngularVelocity;

            ObjectPart.PointLightParam lp = objpart.PointLight;
            p["LightEnabled"] = lp.IsLight;
            p["LightColor"] = lp.LightColor;
            p["LightIntensity"] = lp.Intensity;
            p["LightRadius"] = lp.Radius;
            p["LightFalloff"] = lp.Falloff;

            ObjectPart.TextParam tp = objpart.Text;
            p["HoverText"] = tp.Text;
            p["HoverTextColor"] = tp.TextColor;

            ObjectPart.FlexibleParam fp = objpart.Flexible;
            p["IsFlexible"] = fp.IsFlexible;
            p["FlexibleFriction"] = fp.Friction;
            p["FlexibleGravity"] = fp.Gravity;
            p["FlexibleSoftness"] = fp.Softness;
            p["FlexibleWind"] = fp.Wind;
            p["FlexibleForce"] = fp.Force;

            ObjectPart.SoundParam sound = objpart.Sound;
            p["LoopedSound"] = sound.SoundID;
            p["SoundRadius"] = sound.Radius;
            p["SoundGain"] = sound.Gain;
            p["SoundFlags"] = sound.Flags;

            ObjectPart.CollisionSoundParam collisionsound = objpart.CollisionSound;
            p["ImpactSound"] = collisionsound.ImpactSound;
            p["ImpactVolume"] = collisionsound.ImpactVolume;

            ObjectPart.PrimitiveShape ps = objpart.Shape;
            p["IsShapeSculptInverted"] = ps.IsSculptInverted;
            p["IsShapeSculptMirrored"] = ps.IsSculptMirrored;
            p["ShapeSculptMap"] = ps.SculptMap;
            p["ShapeSculptType"] = (int)ps.SculptType;
            p["ShapeType"] = (int)ps.Type;
            p["PathBegin"] = ps.PathBegin;
            p["PathCurve"] = ps.PathCurve;
            p["PathEnd"] = ps.PathEnd;
            p["PathRadiusOffset"] = ps.PathRadiusOffset;
            p["PathRevolutions"] = ps.PathRevolutions;
            p["PathScaleX"] = ps.PathScaleX;
            p["PathScaleY"] = ps.PathScaleY;
            p["PathShearX"] = ps.PathShearX;
            p["PathshearY"] = ps.PathShearY;
            p["PathSkew"] = ps.PathSkew;
            p["PathTaperX"] = ps.PathTaperX;
            p["PathTaperY"] = ps.PathTaperY;
            p["PathTwist"] = ps.PathTwist;
            p["PathTwistBegin"] = ps.PathTwistBegin;
            p["ProfileBegin"] = ps.ProfileBegin;
            p["ProfileCurve"] = ps.ProfileCurve;
            p["ProfileEnd"] = ps.ProfileEnd;
            p["ProfileHollow"] = ps.ProfileHollow;
            p["IsShapeSculptInverted"] = ps.IsSculptInverted ? 1 : 0;
            p["IsShapeSculptMirrored"] = ps.IsSculptMirrored ? 1 : 0;
            p["ShapeSculptMap"] = ps.SculptMap;
            p["ShapeSculptType"] = (int)ps.SculptType;
            p["ShapeType"] = (int)ps.Type;
            p["PCode"] = (int)ps.PCode;

            p["ExtraParams"] = objpart.ExtraParamsBytes;

            p["ParticleSystem"] = objpart.ParticleSystemBytes;

            p["TextureEntryBytes"] = objpart.TextureEntryBytes;
            p["TextureAnimationBytes"] = objpart.TextureAnimationBytes;

            p["ScriptAccessPin"] = objpart.ScriptAccessPin;

            using(MemoryStream ms = new MemoryStream())
            { 
                LLSD_Binary.Serialize(objpart.DynAttrs, ms);
                p["DynAttrs"] = ms.GetBuffer();
            }

            MySQLUtilities.ReplaceInsertInto(connection, "prims", p);
        }

        #endregion

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

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
                "),"

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
            "ALTER TABLE %tablename% ADD COLUMN (DynAttrs LONGBLOB),"
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
                "ALTER TABLE %tablename% ADD COLUMN (TextureEntryBytes BLOB, TextureAnimationBytes BLOB),"
        };
        #endregion
    }
}
