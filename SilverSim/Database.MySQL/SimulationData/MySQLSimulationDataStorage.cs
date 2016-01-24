// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System;

namespace SilverSim.Database.MySQL.SimulationData
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("MySQL Simulation Data Backend")]
    public sealed partial class MySQLSimulationDataStorage : SimulationDataStorageInterface, IDBServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");
        readonly string m_ConnectionString;
        readonly MySQLSimulationDataObjectStorage m_ObjectStorage;
        readonly MySQLSimulationDataParcelStorage m_ParcelStorage;
        readonly MySQLSimulationDataScriptStateStorage m_ScriptStateStorage;
        readonly MySQLSimulationDataTerrainStorage m_TerrainStorage;
        readonly MySQLSimulationDataEnvSettingsStorage m_EnvironmentStorage;
        readonly MySQLSimulationDataRegionSettingsStorage m_RegionSettingsStorage;
        readonly MySQLSimulationDataSpawnPointStorage m_SpawnPointStorage;
        readonly MySQLSimulationDataLightShareStorage m_LightShareStorage;
        readonly MySQLSimulationDataEnvControllerStorage m_EnvironmentControllerStorage;

        #region Constructor
        public MySQLSimulationDataStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ObjectStorage = new MySQLSimulationDataObjectStorage(connectionString);
            m_ParcelStorage = new MySQLSimulationDataParcelStorage(connectionString);
            m_TerrainStorage = new MySQLSimulationDataTerrainStorage(connectionString);
            m_ScriptStateStorage = new MySQLSimulationDataScriptStateStorage(connectionString);
            m_EnvironmentStorage = new MySQLSimulationDataEnvSettingsStorage(connectionString);
            m_RegionSettingsStorage = new MySQLSimulationDataRegionSettingsStorage(connectionString);
            m_SpawnPointStorage = new MySQLSimulationDataSpawnPointStorage(connectionString);
            m_LightShareStorage = new MySQLSimulationDataLightShareStorage(connectionString);
            m_EnvironmentControllerStorage = new MySQLSimulationDataEnvControllerStorage(connectionString);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        #region Properties
        public override SimulationDataEnvControllerStorageInterface EnvironmentController
        {
            get
            {
                return m_EnvironmentControllerStorage;
            }
        }

        public override SimulationDataLightShareStorageInterface LightShare
        {
            get
            {
                return m_LightShareStorage;
            }
        }

        public override SimulationDataSpawnPointStorageInterface Spawnpoints
        {
            get
            {
                return m_SpawnPointStorage;
            }
        }

        public override SimulationDataEnvSettingsStorageInterface EnvironmentSettings
        {
            get 
            {
                return m_EnvironmentStorage;
            }
        }

        public override SimulationDataObjectStorageInterface Objects
        {
            get
            {
                return m_ObjectStorage;
            }
        }

        public override SimulationDataParcelStorageInterface Parcels
        {
            get
            {
                return m_ParcelStorage;
            }
        }
        public override SimulationDataScriptStateStorageInterface ScriptStates
        {
            get 
            {
                return m_ScriptStateStorage;
            }
        }

        public override SimulationDataTerrainStorageInterface Terrains
        {
            get 
            {
                return m_TerrainStorage;
            }
        }

        public override SimulationDataRegionSettingsStorageInterface RegionSettings
        {
            get
            {
                return m_RegionSettingsStorage;
            }
        }
        #endregion

        #region IDBServiceInterface
        public void VerifyConnection()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }
        #endregion

        static readonly string[] Tables = new string[]
        {
            "primitems",
            "prims",
            "objects",
            "scriptstates",
            "terrains",
            "parcels",
            "environmentsettings",
            "environmentcontroller",
            "regionsettings",
            "lightshare",
            "spawnpoints"
        };

        public override void RemoveRegion(UUID regionID)
        {

            string regionIdStr = regionID.ToString();
            foreach (string table in Tables)
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + table + " WHERE RegionID LIKE '" + regionIdStr + "'", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("SimulationData")]
    public class MySQLSimulationDataServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");
        public MySQLSimulationDataServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLSimulationDataStorage(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
