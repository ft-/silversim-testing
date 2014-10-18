/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataObjectStorage : SimulationDataObjectStorageInterface
    {
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
                using(MySqlCommand cmd = new MySqlCommand("SELECT ID FROM objects WHERE RegionID LIKE ?id ORDER BY LinkNumber", connection))
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
                            item.GroupID = (string)dbReader["GroupID"];
                            item.GroupOwned = (int)dbReader["GroupOwned"] != 0;
                            item.ID = (string)dbReader["InventoryID"];
                            item.InventoryType = (InventoryType)(int)dbReader["InventoryType"];
                            item.LastOwner = new UUI((string)dbReader["LastOwner"]);
                            item.Name = new UUI((string)dbReader["Name"]);
                            item.Owner = new UUI((string)dbReader["Owner"]);
                            item.ParentFolderID = (string)dbReader["ParentFolderID"];
                            item.Permissions.Base = (InventoryItem.PermissionsMask)(uint)dbReader["BasePermissions"];
                            item.Permissions.Current = (InventoryItem.PermissionsMask)(uint)dbReader["CurrentPermissions"];
                            item.Permissions.EveryOne = (InventoryItem.PermissionsMask)(uint)dbReader["EveryOnePermissions"];
                            item.Permissions.Group = (InventoryItem.PermissionsMask)(uint)dbReader["GroupPermissions"];
                            item.Permissions.NextOwner = (InventoryItem.PermissionsMask)(uint)dbReader["NextOwnerPermissions"];
                            item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType)(int)dbReader["SaleType"];
                            item.SaleInfo.Price = (int)dbReader["SalePrice"];
                            item.SaleInfo.PermMask = (InventoryItem.PermissionsMask)(uint)dbReader["SalePermMask"];
                            objpart.Inventory.Add(item.ID, item.Name, item);
                        }
                    }
                }
            }
        }

        public override ObjectGroup this[UUID key]
        {
            get
            {
                ObjectGroup objgroup;
                using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM prims WHERE RootPartID LIKE ?id ORDER BY LinkNumber", connection))
                    {
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
                            objgroup.Owner = new UUI((string)dbReader["Owner"]);
                            objgroup.LastOwner = new UUI((string)dbReader["LastOwner"]);
                            objgroup.Creator = new UUI((string)dbReader["Creator"]);
                            objgroup.GroupID = (string)dbReader["GroupID"];
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
                                objpart.ID = (string)dbReader["ID"];
                                objpart.Position = MySQLUtilities.GetVector(dbReader, "Position");
                                objpart.Rotation = MySQLUtilities.GetQuaternion(dbReader, "Rotation");
                                objpart.SitText = (string)dbReader["SitText"];
                                objpart.TouchText = (string)dbReader["TouchText"];
                                objpart.Name = (string)dbReader["Name"];
                                objpart.Description = (string)dbReader["Description"];
                                objpart.SitTargetOffset = MySQLUtilities.GetVector(dbReader, "SitTargetOffset");
                                objpart.SitTargetOrientation = MySQLUtilities.GetQuaternion(dbReader, "SitTargetOrientation");

                                objpart.PhysicsShapeType = (PrimitivePhysicsShapeType)(int)dbReader["ShapeType"];
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
                                tp.Text = (string)dbReader["Text"];
                                tp.TextColor = MySQLUtilities.GetColorAlpha(dbReader, "TextColor");
                                objpart.Text = tp;

                                ObjectPart.FlexibleParam fp = new ObjectPart.FlexibleParam();
                                fp.IsFlexible = MySQLUtilities.GetBoolean(dbReader, "IsFlexible");
                                fp.Friction = (double)dbReader["FlexibleFriction"];
                                fp.Gravity = (double)dbReader["FlexibleGravity"];
                                fp.Softness = (int)dbReader["FlexibleSoftness"];
                                fp.Wind = (double)dbReader["FlexibleWind"];
                                fp.Force = MySQLUtilities.GetVector(dbReader, "FlexibleForce");
                                objpart.Flexible = fp;

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
                                ps.IsSculptInverted = MySQLUtilities.GetBoolean(dbReader, "IsShapeSculptInverted");
                                ps.IsSculptMirrored = MySQLUtilities.GetBoolean(dbReader, "IsShapeSculptMirrored");
                                ps.SculptMap = (string)dbReader["ShapeSculptMap"];
                                ps.SculptType = (PrimitiveSculptType)(int)dbReader["ShapeSculptType"];
                                ps.Type = (PrimitiveShapeType)(int)dbReader["ShapeType"];
                                objpart.Shape = ps;

                                objpart.ParticleSystemBytes = MySQLUtilities.GetBytes(dbReader, "ParticleSystem");

                                objpart.ScriptAccessPin = (int)dbReader["ScriptAccessPin"];

                                LoadInventory(objpart);
                                objgroup.Add((int)dbReader["LinkNumber"], objpart.ID, objpart);
                            }
                        }
                    }
                }
                return objgroup;
            }
        }

        public override void UpdateObjectGroup(ObjectGroup objgroup)
        {

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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE ID LIKE ?id", connection))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM prims WHERE ID LIKE ?id", connection))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void DeleteObjectGroup(UUID obj)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM prims WHERE RootPartID LIKE ?id"))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM objects WHERE RootPartID LIKE ?id"))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
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
            p["GroupID"] = item.GroupID;
            p["GroupOwned"] = item.GroupOwned;
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

            MySQLUtilities.ReplaceInsertInto(connection, "primitems", p);
        }

        private void UpdateObjectPart(MySqlConnection connection, ObjectPart objpart)
        {
            if(objpart.Group.IsTemporary || objpart.Group.IsTempOnRez)
            {
                return;
            }
            Dictionary<string, object> p = new Dictionary<string, object>();
            p["ID"] = objpart.ID;
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

            p["AngularVelocity"] = objpart.AngularVelocity;

            ObjectPart.PointLightParam lp = objpart.PointLight;
            p["LightEnabled"] = lp.IsLight;
            p["LightColor"] = lp.LightColor;
            p["LightIntensity"] = lp.Intensity;
            p["LightRadius"] = lp.Radius;
            p["LightFalloff"] = lp.Falloff;

            ObjectPart.TextParam tp = objpart.Text;
            p["Text"] = tp.Text;
            p["TextColor"] = tp.TextColor;

            ObjectPart.FlexibleParam fp = objpart.Flexible;
            p["IsFlexible"] = fp.IsFlexible;
            p["FlexibleFriction"] = fp.Friction;
            p["FlexibleGravity"] = fp.Gravity;
            p["FlexibleSoftness"] = fp.Softness;
            p["FlexibleWind"] = fp.Wind;
            p["FlexibleForce"] = fp.Force;

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

            p["ParticleSystem"] = objpart.ParticleSystemBytes;

            p["ScriptAccessPin"] = objpart.ScriptAccessPin;

            MySQLUtilities.ReplaceInsertInto(connection, "prims", p);
        }

        #endregion
    }
}
