// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.ServiceInterfaces.Database;

namespace SilverSim.Database.MySQL.SimulationData
{
    #region Service Implementation
    public class MySQLSimulationDataStorage : SimulationDataStorageInterface, IDBServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");
        private string m_ConnectionString;
        private MySQLSimulationDataObjectStorage m_ObjectStorage;
        private MySQLSimulationDataParcelStorage m_ParcelStorage;
        private MySQLSimulationDataScriptStateStorage m_ScriptStateStorage;
        private MySQLSimulationDataTerrainStorage m_TerrainStorage;
        private MySQLSimulationDataEnvSettingsStorage m_EnvironmentStorage;

        #region Constructor
        public MySQLSimulationDataStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ObjectStorage = new MySQLSimulationDataObjectStorage(connectionString);
            m_ParcelStorage = new MySQLSimulationDataParcelStorage(connectionString);
            m_TerrainStorage = new MySQLSimulationDataTerrainStorage(connectionString);
            m_ScriptStateStorage = new MySQLSimulationDataScriptStateStorage(connectionString);
            m_EnvironmentStorage = new MySQLSimulationDataEnvSettingsStorage(connectionString);
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        #region Properties
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
        #endregion

        #region IDBServiceInterface
        public void VerifyConnection()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }

        public void ProcessMigrations()
        {
            m_ObjectStorage.ProcessMigrations();
            m_ScriptStateStorage.ProcessMigrations();
            m_ParcelStorage.ProcessMigrations();
            m_TerrainStorage.ProcessMigrations();
            m_EnvironmentStorage.ProcessMigrations();
        }
        #endregion
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
