// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Database.MySQL.SimulationData
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("MySQL Simulation Data Backend")]
    public sealed partial class MySQLSimulationDataStorage : SimulationDataStorageInterface, IDBServiceInterface, IPlugin, IPluginShutdown
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
        }

        public void Startup(ConfigurationLoader loader)
        {
            StartStorageThread();
        }
        #endregion

        #region Properties
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

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutDatabase;
            }
        }

        public void Shutdown()
        {
            StopStorageThread();
        }

        public override void RemoveRegion(UUID regionID)
        {
            List<UUID> objects = Objects.ObjectsInRegion(regionID);
            List<UUID> prims = Objects.PrimitivesInRegion(regionID);
            foreach(UUID prim in prims)
            {
                m_ObjectStorage.DeleteObjectPart(prim);
            }
            foreach(UUID objid in objects)
            {
                m_ObjectStorage.DeleteObjectGroup(objid);
            }

            string regionIdStr = regionID.ToString();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM scriptstates WHERE RegionID LIKE '" + regionIdStr + "'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM terrains WHERE RegionID LIKE '" + regionIdStr + "'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM parcels WHERE RegionID LIKE '" + regionIdStr + "'", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            EnvironmentSettings.Remove(regionID);
            Spawnpoints.Remove(regionID);
            LightShare.Remove(regionID);
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
