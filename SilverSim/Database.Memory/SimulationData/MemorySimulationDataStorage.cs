// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System;

namespace SilverSim.Database.Memory.SimulationData
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("Memory Simulation Data Backend")]
    public sealed partial class MemorySimulationDataStorage : SimulationDataStorageInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MEMORY SIMULATION STORAGE");

        #region Constructor
        public MemorySimulationDataStorage()
        {
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
        public override ISimulationDataScriptStateStorageInterface ScriptStates
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

        public override void RemoveRegion(UUID regionID)
        {
            RemoveAllScriptStatesInRegion(regionID);
            RegionSettings.Remove(regionID);
            RemoveTerrain(regionID);
            RemoveAllParcelsInRegion(regionID);
            EnvironmentController.Remove(regionID);
            LightShare.Remove(regionID);
            Spawnpoints.Remove(regionID);
            EnvironmentSettings.Remove(regionID);
            RemoveAllObjectsInRegion(regionID);
        }
    }
    #endregion

    #region Factory
    [PluginName("SimulationData")]
    public class MemorySimulationDataServiceFactory : IPluginFactory
    {
        public MemorySimulationDataServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemorySimulationDataStorage();
        }
    }
    #endregion
}
