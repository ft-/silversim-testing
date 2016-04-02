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

namespace SilverSim.Database.MySQL.SimulationData
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("MySQL Simulation Data Backend")]
    public sealed partial class MySQLSimulationDataStorage : SimulationDataStorageInterface, IDBServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");
        readonly string m_ConnectionString;

        #region Constructor
        public MySQLSimulationDataStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_WhiteListStorage = new MySQLSimulationDataParcelAccessListStorage(connectionString, "parcelaccesswhitelist");
            m_BlackListStorage = new MySQLSimulationDataParcelAccessListStorage(connectionString, "parcelaccessblacklist");
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        #region Properties
        public override ISimulationDataEnvControllerStorageInterface EnvironmentController
        {
            get
            {
                return this;
            }
        }

        public override ISimulationDataLightShareStorageInterface LightShare
        {
            get
            {
                return this;
            }
        }

        public override ISimulationDataSpawnPointStorageInterface Spawnpoints
        {
            get
            {
                return this;
            }
        }

        public override ISimulationDataEnvSettingsStorageInterface EnvironmentSettings
        {
            get 
            {
                return this;
            }
        }

        public override ISimulationDataObjectStorageInterface Objects
        {
            get
            {
                return this;
            }
        }

        public override ISimulationDataParcelStorageInterface Parcels
        {
            get
            {
                return this;
            }
        }
        public override IISimulationDataScriptStateStorageInterface ScriptStates
        {
            get 
            {
                return this;
            }
        }

        public override ISimulationDataTerrainStorageInterface Terrains
        {
            get 
            {
                return this;
            }
        }

        public override ISimulationDataRegionSettingsStorageInterface RegionSettings
        {
            get
            {
                return this;
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
