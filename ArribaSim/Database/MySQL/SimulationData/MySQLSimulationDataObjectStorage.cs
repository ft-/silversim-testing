using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Scene.ServiceInterfaces.SimulationData;
using ArribaSim.Types;
using ArribaSim.Types.Asset;
using ArribaSim.Types.Inventory;
using ArribaSim.Scene.Types.Object;
using MySql.Data.MySqlClient;

namespace ArribaSim.Database.MySQL.SimulationData
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
                using(MySqlCommand cmd = new MySqlCommand("SELECT ID FROM prims WHERE RootPartID = ID ORDER BY LinkNumber", connection))
                {
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
                            item.CreationDate = Date.UnixTimeToDateTime((ulong)dbReader["CreationDate"]);
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
                            item.Permissions.Base = (uint)dbReader["BasePermissions"];
                            item.Permissions.Current = (uint)dbReader["CurrentPermissions"];
                            item.Permissions.EveryOne = (uint)dbReader["EveryOnePermissions"];
                            item.Permissions.Group = (uint)dbReader["GroupPermissions"];
                            item.Permissions.NextOwner = (uint)dbReader["NextOwnerPermissions"];
                            item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType)(int)dbReader["SaleType"];
                            item.SaleInfo.Price = (int)dbReader["SalePrice"];
                            item.SaleInfo.PermMask = (uint)dbReader["SalePermMask"];
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
                    using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM prims WHERE RootPartID LIKE ?id ORDER BY LinkNumber", connection))
                    {
                        cmd.Parameters.AddWithValue("?id", key);
                        using(MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            objgroup = new ObjectGroup();

                            while(dbReader.Read())
                            {
                                ObjectPart objpart = new ObjectPart();
                                objpart.ID = (string)dbReader["ID"];
                                objpart.Position = (string)dbReader["Position"];
                                objpart.Rotation = new AString((string)dbReader["Rotation"]).AsQuaternion;
                                objpart.SitText = (string)dbReader["SitText"];
                                objpart.TouchText = (string)dbReader["TouchText"];
                                objpart.Name = (string)dbReader["Name"];
                                objpart.Description = (string)dbReader["Description"];
                                objpart.SitTargetOffset = (string)dbReader["SitTargetOffset"];
                                objpart.SitTargetOrientation = new AString((string)dbReader["SitTargetOrientation"]).AsQuaternion;

                                objpart.PhysicsShapeType = (PrimitivePhysicsShapeType)(int)dbReader["ShapeType"];
                                objpart.Material = (PrimitiveMaterial)(int)dbReader["Material"];
                                objpart.Size = (string)dbReader["Size"];
                                objpart.Slice = (string)dbReader["Slice"];
                                

                                ObjectPart.OmegaParam op = new ObjectPart.OmegaParam();
                                op.Axis = (string)dbReader["OmegaAxis"];
                                op.Spinrate = (float)dbReader["OmegaSpinRate"];
                                op.Gain = (float)dbReader["OmegaGain"];
                                objpart.Omega = op;

                                ObjectPart.PointLightParam lp = new ObjectPart.PointLightParam();
                                lp.IsLight = ((int) dbReader["LightEnabled"]) != 0;
                                lp.LightColor = new Color(
                                    (double)dbReader["LightColorRed"],
                                    (double)dbReader["LightColorGreen"],
                                    (double)dbReader["LightColorBlue"]);
                                lp.Intensity = (double)dbReader["LightIntensity"];
                                lp.Radius = (double)dbReader["LightRadius"];
                                lp.Falloff = (double)dbReader["LightFalloff"];
                                objpart.PointLight = lp;

                                ObjectPart.TextParam tp = new ObjectPart.TextParam();
                                tp.Text = (string)dbReader["Text"];
                                tp.TextColor = new ColorAlpha(
                                    (double)dbReader["TextColorRed"], 
                                    (double)dbReader["TextColorGreen"],
                                    (double)dbReader["TextColorBlue"],
                                    (double)dbReader["TextColorAlpha"]);
                                objpart.Text = tp;

                                ObjectPart.FlexibleParam fp = new ObjectPart.FlexibleParam();
                                fp.IsFlexible = (int)dbReader["IsFlexible"] != 0;
                                fp.Friction = (double)dbReader["FlexibleFriction"];
                                fp.Gravity = (double)dbReader["FlexibleGravity"];
                                fp.Softness = (int)dbReader["FlexibleSoftness"];
                                fp.Wind = (double)dbReader["FlexibleWind"];
                                fp.Force = (string)dbReader["FlexibleForce"];
                                objpart.Flexible = fp;

                                ObjectPart.PrimitiveShape ps = new ObjectPart.PrimitiveShape();
                                ps.AdvancedCut = (string)dbReader["ShapeAdvancedCut"];
                                ps.Cut = (string)dbReader["ShapeCut"];
                                ps.Dimple = (string)dbReader["ShapeDimple"];
                                ps.HoleShape = (PrimitiveHoleShape)(int)dbReader["ShapeHoleShape"];
                                ps.HoleSize = (string)dbReader["ShapeHoleSize"];
                                ps.Hollow = (double)dbReader["ShapeHollow"];
                                ps.IsSculptInverted = (int)dbReader["IsShapeSculptInverted"] != 0;
                                ps.IsSculptMirrored = (int)dbReader["IsShapeSculptMirrored"] != 0;
                                ps.RadiusOffset = (double)dbReader["ShapeRadiusOffset"];
                                ps.Revolutions = (double)dbReader["ShapeRevolutions"];
                                ps.SculptMap = (string)dbReader["ShapeSculptMap"];
                                ps.SculptType = (PrimitiveSculptType)(int)dbReader["ShapeSculptType"];
                                ps.Skew = (double)dbReader["ShapeSkew"];
                                ps.Taper = (string)dbReader["ShapeTaper"];
                                ps.TopShear = (string)dbReader["ShapeTopShear"];
                                ps.TopSize = (string)dbReader["ShapeTopSize"];
                                ps.Twist = (string)dbReader["ShapeTwist"];
                                ps.Type = (PrimitiveShapeType)(int)dbReader["ShapeType"];
                                objpart.Shape = ps;

                                LoadInventory(objpart);
                                objgroup.Add((int)dbReader["LinkNumber"], objpart.ID, objpart);
                            }
                        }
                    }
                }
                return objgroup;
            }
        }


        public override void StoreObject(ObjectGroup objgroup)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public override void StoreObjectInventory(ObjectGroup objgroup)
        {
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                /*
                foreach(ObjectPart part in objgroup)
                {
                    foreach(ObjectPartInventoryItem item in part.Inventory)
                    {
//                        MySQLUtilities.ReplaceInsertInto(connection, "primitems", );
                    }
                }
                 * */
            }
        }

        public override void UpdateObjectPart(ObjectPart objpart)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public override void UpdateObjectPartInventory(ObjectPart objpart)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public override void DeleteObject(UUID obj)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE ID LIKE ?id", connection))
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primitems WHERE RootPartID LIKE ?id"))
                {
                    cmd.Parameters.AddWithValue("?id", obj);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
