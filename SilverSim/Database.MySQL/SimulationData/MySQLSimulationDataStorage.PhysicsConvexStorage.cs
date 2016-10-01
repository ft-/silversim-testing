// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    partial class MySQLSimulationDataStorage : ISimulationDataPhysicsConvexStorageInterface
    {
        static readonly IMigrationElement[] Migrations_Physics = new IMigrationElement[]
        {
            #region Table sculptmeshphysics
            new SqlTable("meshphysics") {IsDynamicRowFormat = true },
            new AddColumn<UUID>("MeshID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<byte[]>("ConvexData") { IsLong = true },
            new PrimaryKeyInfo(new string[] {"MeshID" }),
            #endregion

            #region Table primphysics
            new SqlTable("primphysics") {IsDynamicRowFormat = true },
            new AddColumn<byte[]>("ShapeKey") { Cardinality = 255 },
            new AddColumn<byte[]>("ConvexData") { IsLong = true },
            new PrimaryKeyInfo(new string[] {"ShapeKey" }),
            #endregion
        };

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[UUID meshid]
        {
            get
            {
                PhysicsConvexShape shape;
                if(!((ISimulationDataPhysicsConvexStorageInterface)this).TryGetValue(meshid, out shape))
                {
                    throw new KeyNotFoundException();
                }
                return shape;
            }
            set
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param["MeshID"] = meshid;
                    param["ConvexData"] = value.SerializedData;
                    conn.ReplaceInto("sculptmeshphysics", param);
                }
            }
        }

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[ObjectPart.PrimitiveShape primShape]
        {
            get
            {
                PhysicsConvexShape shape;
                if (!((ISimulationDataPhysicsConvexStorageInterface)this).TryGetValue(primShape, out shape))
                {
                    throw new KeyNotFoundException();
                }
                return shape;
            }
            set
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param["ShapeKey"] = primShape.Serialization;
                    param["ConvexData"] = value.SerializedData;
                    conn.ReplaceInto("primphysics", param);
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(UUID meshid, out PhysicsConvexShape shape)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ConvexData FROM meshphysics WHERE MeshID=?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", meshid);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            shape = new PhysicsConvexShape();
                            shape.SerializedData = dbReader.GetBytes("ConvexData");
                            return true;
                        }
                    }
                }
            }

            shape = null;
            return false;
        }

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(ObjectPart.PrimitiveShape primShape, out PhysicsConvexShape shape)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ConvexData FROM primphysics WHERE ShapeKey=?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", primShape.Serialization);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            shape = new PhysicsConvexShape();
                            shape.SerializedData = dbReader.GetBytes("ConvexData");
                            return true;
                        }
                    }
                }
            }

            shape = null;
            return false;
        }

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(UUID sculptmeshid)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT NULL FROM meshphysics WHERE MeshID=?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", sculptmeshid);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(ObjectPart.PrimitiveShape primShape)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT NULL FROM primphysics WHERE ShapeKey=?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", primShape.Serialization);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(UUID sculptmeshid)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM meshphysics WHERE MeshID=?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", sculptmeshid);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(ObjectPart.PrimitiveShape primShape)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM primphysics WHERE ShapeKey=?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", primShape.Serialization);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        ICollection<UUID> ISimulationDataPhysicsConvexStorageInterface.KnownMeshIds
        {
            get
            {
                List<UUID> sculptids = new List<UUID>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT MeshID FROM meshphysics", conn))
                    {
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while(dbReader.Read())
                            {
                                sculptids.Add(dbReader.GetUUID("MeshID"));
                            }
                        }
                    }
                }
                return sculptids;
            }
        }
    }
}