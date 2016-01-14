// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataTerrainStorage : SimulationDataTerrainStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");

        readonly string m_ConnectionString;
        public MySQLSimulationDataTerrainStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override List<LayerPatch> this[UUID regionID]
        {
            get
            {
                List<LayerPatch> patches = new List<LayerPatch>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT PatchID, TerrainData FROM terrains WHERE RegionID LIKE '" + regionID.ToString() + "'", connection))
                    {
                        cmd.CommandTimeout = 3600;
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            LayerPatch patch;

                            while (dbReader.Read())
                            {
                                patch = new LayerPatch();
                                patch.ExtendedPatchID = dbReader.GetUInt32("PatchID");
                                patch.Serialization = dbReader.GetBytes("TerrainData");
                                patches.Add(patch);
                            }
                        }
                    }
                }
                return patches;
            }
        }
    }
}
