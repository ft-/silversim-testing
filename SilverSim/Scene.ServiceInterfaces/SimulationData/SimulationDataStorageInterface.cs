// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System.Threading;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface
    {
        #region Constructor
        protected SimulationDataStorageInterface()
        {
        }
        #endregion

        public abstract ISimulationDataEnvControllerStorageInterface EnvironmentController
        {
            get;
        }

        public abstract ISimulationDataSpawnPointStorageInterface Spawnpoints
        {
            get;
        }

        public abstract ISimulationDataLightShareStorageInterface LightShare
        {
            get;
        }

        public abstract ISimulationDataObjectStorageInterface Objects
        {
            get;
        }

        public abstract ISimulationDataParcelStorageInterface Parcels
        {
            get;
        }

        public abstract IISimulationDataScriptStateStorageInterface ScriptStates
        {
            get;
        }

        public abstract ISimulationDataTerrainStorageInterface Terrains
        {
            get;
        }

        public abstract ISimulationDataRegionSettingsStorageInterface RegionSettings
        {
            get;
        }

        public abstract ISimulationDataEnvSettingsStorageInterface EnvironmentSettings
        {
            get;
        }


        public abstract void RemoveRegion(UUID regionID);


        public abstract class SceneListener : ISceneListener
        {
            protected bool m_StopStorageThread;
            protected readonly BlockingQueue<ObjectUpdateInfo> m_StorageMainRequestQueue = new BlockingQueue<ObjectUpdateInfo>();

            public void StopStorageThread()
            {
                m_StopStorageThread = true;
            }

            public void StartStorageThread()
            {
                new Thread(StorageMainThread).Start();
            }

            protected abstract void StorageMainThread();

            public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
            {
                m_StorageMainRequestQueue.Enqueue(info);
            }
        }

        public abstract SceneListener GetSceneListener(UUID regionID);

        public abstract class TerrainListener : ITerrainListener
        {
            protected bool m_StopStorageThread;
            protected readonly BlockingQueue<LayerPatch> m_StorageTerrainRequestQueue = new BlockingQueue<LayerPatch>();

            public void StopStorageThread()
            {
                m_StopStorageThread = true;
            }

            public void StartStorageThread()
            {
                new Thread(StorageTerrainThread).Start();
            }

            protected abstract void StorageTerrainThread();

            public void TerrainUpdate(LayerPatch layerpath)
            {
                m_StorageTerrainRequestQueue.Enqueue(layerpath);
            }
        }

        public abstract TerrainListener GetTerrainListener(UUID regionID);
    }
}
