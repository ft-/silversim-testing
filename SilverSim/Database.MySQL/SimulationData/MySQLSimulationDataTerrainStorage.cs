// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataTerrainStorage : SimulationDataTerrainStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");

        public string m_ConnectionString;
        public MySQLSimulationDataTerrainStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override LayerPatch this[UUID regionID, uint extendedPatchID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT TerrainData FROM terrains WHERE RegionID LIKE ?regionid AND PatchID = ?patchid", connection))
                    {
                        cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                        cmd.Parameters.AddWithValue("?patchid", extendedPatchID);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            LayerPatch patch;

                            if (dbReader.Read())
                            {
                                patch = new LayerPatch();
                                patch.ExtendedPatchID = extendedPatchID;
                                patch.Serialization = (byte[])dbReader["TerrainData"];
                                return patch;
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }

            set
            {
                if(value.ExtendedPatchID != extendedPatchID)
                {
                    throw new ArgumentException("value.ExtendedPatchID != extendedPatchID");
                }
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();

                    using (MySqlCommand cmd =
                        new MySqlCommand(
                            "REPLACE INTO terrains (RegionID, PatchID, TerrainData)" +
                            "VALUES(?regionid, ?patchid, ?terraindata)",
                            conn))
                    {
                        try
                        {
                            using (cmd)
                            {
                                cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                                cmd.Parameters.AddWithValue("?patchid", extendedPatchID);
                                cmd.Parameters.AddWithValue("?terraindata", value.Serialization);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception e)
                        {
                            m_Log.Error(
                                string.Format("MySQL failure creating terrain segment {1} for region {0}.  Exception  ",
                                    regionID, extendedPatchID)
                                , e);
                            throw new TerrainStorageFailedException(regionID, extendedPatchID);
                        }
                    }
                }
            }
        }

        public override List<LayerPatch> this[UUID regionID]
        {
            get
            {
                List<LayerPatch> patches = new List<LayerPatch>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT PatchID, TerrainData FROM terrains WHERE RegionID LIKE ?id", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        cmd.Parameters.AddWithValue("?id", regionID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            LayerPatch patch;

                            while (dbReader.Read())
                            {
                                patch = new LayerPatch();
                                patch.ExtendedPatchID = (uint)dbReader["PatchID"];
                                patch.Serialization = (byte[])dbReader["TerrainData"];
                                patches.Add(patch);
                            }
                        }
                    }
                }
                return patches;
            }
        }

        #region Migrations
        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "terrains", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "PatchID INT(11) UNSIGNED NOT NULL," +
                "TerrainData BLOB," +
                "PRIMARY KEY(RegionID, PatchID))"
        };
        #endregion
    }
}
