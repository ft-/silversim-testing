// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using log4net;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Threading;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface
    {
        public abstract ISimulationDataPhysicsConvexStorageInterface PhysicsConvexShapes { get; }

        public abstract ISimulationDataEnvControllerStorageInterface EnvironmentController { get; }

        public abstract ISimulationDataSpawnPointStorageInterface Spawnpoints { get; }

        public abstract ISimulationDataLightShareStorageInterface LightShare { get; }

        public abstract ISimulationDataObjectStorageInterface Objects { get; }

        public abstract ISimulationDataParcelStorageInterface Parcels { get; }

        public abstract ISimulationDataScriptStateStorageInterface ScriptStates { get; }

        public abstract ISimulationDataTerrainStorageInterface Terrains { get; }

        public abstract ISimulationDataRegionSettingsStorageInterface RegionSettings { get; }

        public abstract ISimulationDataEnvSettingsStorageInterface EnvironmentSettings { get; }

        public abstract ISimulationDataRegionExperiencesStorageInterface RegionExperiences { get; }

        public abstract ISimulationDataRegionTrustedExperiencesStorageInterface TrustedExperiences { get; }

        public abstract void RemoveRegion(UUID regionID);

        public abstract class SceneListener : ISceneListener
        {
            public bool IgnorePhysicsLocationUpdates => false;

            protected static readonly ILog m_Log = LogManager.GetLogger("STORAGE SCENE LISTENER");
            protected bool m_StopStorageThread;
            protected readonly BlockingQueue<IUpdateInfo> m_StorageMainRequestQueue = new BlockingQueue<IUpdateInfo>();
            protected readonly UUID m_RegionID;

            public UUID RegionID => m_RegionID;

            protected SceneListener(UUID regionID)
            {
                m_RegionID = regionID;
            }

            public void StopStorageThread()
            {
                m_StopStorageThread = true;
            }

            public void StartStorageThread()
            {
                ThreadManager.CreateThread(StorageMainThread).Start();
            }

            protected abstract void OnUpdate(ObjectInventoryUpdateInfo info);
            protected abstract void OnUpdate(ObjectUpdateInfo info);
            protected abstract void OnIdle();

            protected virtual void OnStart()
            {

            }

            protected virtual void OnStop()
            {

            }

            protected virtual bool HasPendingData
            {
                get
                {
                    return false;
                }
            }

            private void StorageMainThread()
            {
                Thread.CurrentThread.Name = "Storage Main Thread: " + m_RegionID.ToString();
                OnStart();
                int nummessagespending = 0;

                while (!m_StopStorageThread || m_StorageMainRequestQueue.Count != 0 || HasPendingData)
                {
                    IUpdateInfo req;
                    try
                    {
                        req = m_StorageMainRequestQueue.Dequeue(1000);
                    }
                    catch
                    {
                        try
                        {
                            OnIdle();
                            nummessagespending = 0;
                        }
                        catch(Exception e)
                        {
                            m_Log.Error("Exception encountered at OnIdle", e);
                        }
                        continue;
                    }

                    if (nummessagespending > 2000)
                    {
                        try
                        {
                            OnIdle();
                            nummessagespending = 0;
                        }
                        catch (Exception e)
                        {
                            m_Log.Error("Exception encountered at OnIdle", e);
                        }
                    }

                    ObjectUpdateInfo oInfo = req as ObjectUpdateInfo;
                    if (oInfo != null)
                    {
                        try
                        {
                            OnUpdate(oInfo);
                            ++nummessagespending;
                        }
                        catch(Exception e)
                        {
                            m_Log.Error("Inventory item storage encountered exception at " + m_RegionID.ToString(), e);
                        }
                        continue;
                    }

                    ObjectInventoryUpdateInfo iInfo = req as ObjectInventoryUpdateInfo;
                    if (iInfo != null)
                    {
                        try
                        {
                            OnUpdate(iInfo);
                            ++nummessagespending;
                        }
                        catch(Exception e)
                        {
                            m_Log.Error("Inventory storage encountered exception at " + m_RegionID.ToString(), e);
                        }
                    }
                }

                try
                {
                    OnIdle();
                }
                catch(Exception e)
                {
                    m_Log.Error("OnIdle threw an exception after leaving loop", e);
                }

                OnStop();
            }

            public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
            {
                m_StorageMainRequestQueue.Enqueue(info);
            }

            public void ScheduleUpdate(ObjectInventoryUpdateInfo info, UUID fromSceneID)
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
                ThreadManager.CreateThread(StorageTerrainThread).Start();
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
