// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface : ISceneListener
    {
        #region Constructor
        protected SimulationDataStorageInterface()
        {
        }
        #endregion

        public abstract SimulationDataSpawnPointStorageInterface Spawnpoints
        {
            get;
        }

        public abstract SimulationDataLightShareStorageInterface LightShare
        {
            get;
        }

        public abstract SimulationDataObjectStorageInterface Objects
        {
            get;
        }

        public abstract SimulationDataParcelStorageInterface Parcels
        {
            get;
        }

        public abstract SimulationDataScriptStateStorageInterface ScriptStates
        {
            get;
        }

        public abstract SimulationDataTerrainStorageInterface Terrains
        {
            get;
        }

        public abstract SimulationDataRegionSettingsStorageInterface RegionSettings
        {
            get;
        }

        public abstract SimulationDataEnvSettingsStorageInterface EnvironmentSettings
        {
            get;
        }

        protected readonly BlockingQueue<ObjectUpdateInfo> m_StorageMainRequestQueue = new BlockingQueue<ObjectUpdateInfo>();
        protected bool m_StopStorageThread;

        protected void StopStorageThread()
        {
            m_StopStorageThread = true;
        }

        protected void StartStorageThread()
        {
            new Thread(StorageMainThread).Start();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        protected abstract void StorageMainThread();

        public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            m_StorageMainRequestQueue.Enqueue(info);
        }

        public abstract void RemoveRegion(UUID regionID);
    }
}
