// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataStorageInterface : ISceneListener
    {
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

        protected class StorageThreadInfo
        {
            public readonly BlockingQueue<ObjectUpdateInfo> StorageRequestQueue = new BlockingQueue<ObjectUpdateInfo>();
            public readonly RwLockedList<uint> AssignedLocalIDs = new RwLockedList<uint>();
            public StorageThreadInfo()
            {

            }
        }
        protected readonly List<StorageThreadInfo> m_StorageThreads = new List<StorageThreadInfo>();
        protected readonly BlockingQueue<ObjectUpdateInfo> m_StorageMainRequestQueue = new BlockingQueue<ObjectUpdateInfo>();
        protected int m_ActiveStorageRequests;
        protected bool m_StopStorageThread;
        int m_MaxStorageThreads = 50;
        int m_StorageThreadDivider = 10;
        protected readonly RwLockedDictionary<uint, int> m_KnownSerialNumbers = new RwLockedDictionary<uint, int>();

        protected void StopStorageThread()
        {
            m_StopStorageThread = true;
        }

        protected void StartStorageThread()
        {
            new Thread(StorageMainThread).Start();
        }

        void StartStorageThreadInstance(ObjectUpdateInfo info)
        {
            StorageThreadInfo sti = new StorageThreadInfo();
            sti.StorageRequestQueue.Enqueue(info);
            sti.AssignedLocalIDs.Add(info.LocalID);
            new Thread(StorageWorkerThread).Start(sti);
            m_StorageThreads.Add(sti);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        protected void StorageMainThread()
        {
            Thread.CurrentThread.Name = "Storage Main Thread";
            while(!m_StopStorageThread || m_StorageMainRequestQueue.Count != 0)
            {
                ObjectUpdateInfo req;
                try
                {
                    req = m_StorageMainRequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                int serialNumber = req.SerialNumber;
                int knownSerial;
                if(req.IsKilled)
                {
                    /* has to be processed */
                }
                else if (req.Part.SerialNumberLoadedFromDatabase == serialNumber)
                {
                    req.Part.SerialNumberLoadedFromDatabase = 0;
                    if (serialNumber == req.SerialNumber)
                    {
                        /* ignore those */
                        m_KnownSerialNumbers[req.LocalID] = serialNumber;
                        continue;
                    }
                }
                else if (m_KnownSerialNumbers.TryGetValue(req.LocalID, out knownSerial))
                {
                    if (knownSerial == req.SerialNumber && !req.Part.ObjectGroup.IsAttached && !req.Part.ObjectGroup.IsTemporary)
                    {
                        /* ignore it */
                        continue;
                    }
                }
                else if (req.Part.ObjectGroup.IsAttached || req.Part.ObjectGroup.IsTemporary)
                {
                    /* ignore it */
                    continue;
                }

                Interlocked.Increment(ref m_ActiveStorageRequests);
                int reqthreads = m_ActiveStorageRequests / m_StorageThreadDivider + 1;
                if(reqthreads > m_MaxStorageThreads)
                {
                    reqthreads = m_MaxStorageThreads;
                }

                StorageThreadInfo selected_sti = null;
                StorageThreadInfo lowest_sti = null;
                int lowest_count = 0;
                lock (m_StorageThreads)
                {
                    foreach (StorageThreadInfo sti in m_StorageThreads)
                    {
                        if(sti.AssignedLocalIDs.Contains(req.LocalID))
                        {
                            selected_sti = sti;
                            break;
                        }
                        if(sti.StorageRequestQueue.Count < lowest_count || lowest_count == 0)
                        {
                            if (m_StorageThreads.Count >= reqthreads)
                            {
                                lowest_count = sti.StorageRequestQueue.Count;
                                lowest_sti = sti;
                            }
                        }
                    }

                    /* if one request has been scheduled, schedule to same thread */
                    if(selected_sti != null)
                    {
                        selected_sti.StorageRequestQueue.Enqueue(req);
                    }
                    else if(lowest_sti != null)
                    {
                        lowest_sti.AssignedLocalIDs.Add(req.LocalID);
                        lowest_sti.StorageRequestQueue.Enqueue(req);
                    }
                }

                if(lowest_sti == null && selected_sti == null)
                {
                    try
                    {
                        StartStorageThreadInstance(req);
                    }
                    catch
                    {
                        
                    }
                }
            }
        }

        protected abstract void StorageWorkerThread(object p);

        public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            m_StorageMainRequestQueue.Enqueue(info);
        }
    }
}
