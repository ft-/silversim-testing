// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface : ISceneListener
    {
        private readonly ILog m_StorageLog = LogManager.GetLogger("STORAGE THREAD");
        #region Constructor
        public SimulationDataStorageInterface()
        {
        }
        #endregion

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

        public abstract SimulationDataEnvSettingsStorageInterface EnvironmentSettings
        {
            get;
        }

        readonly BlockingQueue<KeyValuePair<ObjectUpdateInfo, UUID>> m_StorageRequestQueue = new BlockingQueue<KeyValuePair<ObjectUpdateInfo, UUID>>();
        bool m_StopStorageThread = false;

        protected void StopStorageThread()
        {
            m_StopStorageThread = true;
        }

        protected void StartStorageThread()
        {
            new Thread(StorageThread).Start();
        }

        protected void StorageThread()
        {
            Thread.CurrentThread.Name = "Storage Thread";
            while(!m_StopStorageThread || m_StorageRequestQueue.Count != 0)
            {
                /* thread always runs until queue is empty it does not stop before */
                KeyValuePair<ObjectUpdateInfo, UUID> req;
                ObjectUpdateInfo info;
                try
                {
                    req = m_StorageRequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                info = req.Key;

                int time = Environment.TickCount;

                if(info.IsKilled)
                {
                    Objects.DeleteObjectPart(info.Part.ID);
                    Objects.DeleteObjectGroup(info.Part.ObjectGroup.ID);
                }
                else if (info.Part.SerialNumberLoadedFromDatabase != info.Part.SerialNumber)
                {
                    ObjectGroup grp = info.Part.ObjectGroup;
                    if(null != grp && !grp.IsTemporary)
                    {
                        Objects.UpdateObjectPart(info.Part);
                    }
                }
                info.Part.SerialNumberLoadedFromDatabase = 0;
                time = Environment.TickCount - time;

                if(m_StorageRequestQueue.Count % 100 == 0)
                {
                    m_StorageLog.InfoFormat("{0} primitives left to store", m_StorageRequestQueue.Count);
                }
            }
        }

        public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            m_StorageRequestQueue.Enqueue(new KeyValuePair<ObjectUpdateInfo, UUID>(info, fromSceneID));
        }
    }
}
