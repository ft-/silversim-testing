// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SilverSim.Viewer.Messages.LayerData;
using System;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface
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
