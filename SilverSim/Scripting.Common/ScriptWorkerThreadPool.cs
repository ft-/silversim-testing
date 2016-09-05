// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Scripting.Common
{
    public sealed class ScriptWorkerThreadPool : IScriptWorkerThreadPool
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SCRIPT WORKER THREAD POOL");

        readonly BlockingQueue<ScriptInstance> m_ScriptTriggerQueue = new BlockingQueue<ScriptInstance>();
        readonly ManualResetEvent m_WaitShutdownEvent = new ManualResetEvent(false);
        private int m_MinimumThreads = 2;
        private int m_MaximumThreads = 150;
        readonly UUID m_SceneID;
        bool m_ShutdownThreads;
        public class ScriptThreadContext
        {
            public ScriptInstance CurrentScriptInstance;
            public Thread ScriptThread;
            public ScriptWorkerThreadPool ThreadPool;

            public ScriptThreadContext()
            {

            }
        }
        readonly RwLockedList<ScriptThreadContext> m_Threads = new RwLockedList<ScriptThreadContext>();

        public int MinimumThreads
        {
            get
            {
                return m_MinimumThreads;
            }
            set
            {
                if(value < 2)
                {
                    throw new ArgumentException("value");
                }
                if(value > m_MaximumThreads)
                {
                    m_MaximumThreads = value;
                }
                m_MinimumThreads = value;
            }
        }

        public int MaximumThreads
        {
            get
            {
                return m_MaximumThreads;
            }
            set
            {
                m_MaximumThreads = value < m_MinimumThreads ? m_MinimumThreads : value;
            }
        }

        public ScriptWorkerThreadPool(int minimumThreads, int maximumThreads, UUID sceneID)
        {
            MinimumThreads = minimumThreads;
            MaximumThreads = maximumThreads;
            m_SceneID = sceneID;

            m_Log.InfoFormat("Starting {0} minimum threads for {1}", minimumThreads, m_SceneID.ToString());
            for (int threadCount = 0; threadCount < m_MinimumThreads; ++threadCount)
            {
                ScriptThreadContext tc = new ScriptThreadContext();
                tc.ScriptThread = new Thread(ThreadMain);
                tc.ScriptThread.Name = "Script Worker: " + m_SceneID.ToString();
                tc.ScriptThread.IsBackground = true;
                tc.ThreadPool = this;
                tc.ScriptThread.Start(tc);
                m_Threads.Add(tc);
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void PostScript(ScriptInstance i)
        {
            /* Do not enqueue the already queued script */
            if(m_ScriptTriggerQueue.Contains(i))
            {
                return;
            }

            bool enqueued = false;
            lock(i)
            {
                if (i.ThreadPool == null)
                {
                    m_ScriptTriggerQueue.Enqueue(i);
                    enqueued = true;
                }
            }

            if(enqueued)
            { 
                int threadsCount = m_Threads.Count;
                if (m_ScriptTriggerQueue.Count > threadsCount && threadsCount < m_MaximumThreads)
                {
                    lock (m_Threads)
                    {
                        try
                        {
                            ScriptThreadContext tc = new ScriptThreadContext();
                            tc.ScriptThread = new Thread(ThreadMain);
                            tc.ThreadPool = this;
                            tc.ScriptThread.Name = "Script Worker: " + m_SceneID.ToString();
                            tc.ScriptThread.IsBackground = true;
                            tc.ScriptThread.Start(tc);
                            m_Threads.Add(tc);
                        }
                        catch
                        {
                            /* do not fail when we could not add a thread */
                        }
                    }
                }
            }
        }

        public void AbortScript(ScriptInstance script)
        {
            m_Threads.ForEach(delegate(ScriptThreadContext tc)
            {
                lock (tc)
                {
                    if (tc.CurrentScriptInstance == script)
                    {
                        /* we can actually abort that script without killing the worker thread here */
                        /* care for the deletion guards here */
                        lock (tc.CurrentScriptInstance)
                        {
                            tc.ScriptThread.Abort();
                        }
                    }
                }
            });
        }

        public void Shutdown()
        {
            m_ShutdownThreads = true;
            if (m_Threads.Count != 0)
            {
                m_Log.InfoFormat("Waiting for script shutdown of region {0}", m_SceneID.ToString());
                if (!m_WaitShutdownEvent.WaitOne(30000))
                {
                    /* we have to abort threads */
                    m_Log.InfoFormat("Killing blocked instances of region {0}", m_SceneID.ToString());
                    foreach(ScriptThreadContext tc in m_Threads)
                    {
                        lock(tc)
                        {
                            ScriptInstance instance = tc.CurrentScriptInstance;
                            if (null != instance)
                            {
                                lock (instance)
                                {
                                    tc.ScriptThread.Abort();
                                }
                            }
                        }
                    }
                }
                m_Log.InfoFormat("Completed script shutdown of region {0}", m_SceneID.ToString());
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void ThreadMain(object obj)
        {
            ScriptThreadContext tc = (ScriptThreadContext)obj;
            ScriptWorkerThreadPool pool = tc.ThreadPool;
            ScriptInstance ev;
            while (!m_ShutdownThreads)
            {
                try
                {
                    ev = pool.m_ScriptTriggerQueue.Dequeue(1000);
                }
                catch
                {
                    lock (m_Threads)
                    {
                        if (m_Threads.Count > m_MinimumThreads || m_ShutdownThreads)
                        {
                            m_Threads.Remove(tc);
                            if (m_ShutdownThreads && m_Threads.Count == 0)
                            {
                                m_WaitShutdownEvent.Set();
                            }
                            return;
                        }
                    }
                    continue;
                }

                try
                {
                    lock (tc)
                    {
                        ev.ThreadPool = this;
                        tc.CurrentScriptInstance = ev;
                    }
                    ev.ProcessEvent();
                }
                catch(ThreadAbortException)
                {
                    /* no in script event should abort us */
                    Thread.ResetAbort();
                    ObjectPartInventoryItem item = ev.Item;
                    ScriptInstance instance = item.ScriptInstance;
                    item.ScriptInstance = null;
                    instance.Remove();
                    ScriptLoader.Remove(item.AssetID, instance);
                    continue;
                }
                catch(ScriptAbortException)
                {
                    ObjectPartInventoryItem item = ev.Item;
                    ScriptInstance instance = item.ScriptInstance;
                    instance.AbortBegin();
                    instance.Remove();
                    item.ScriptInstance = null;
                    ScriptLoader.Remove(item.AssetID, instance);
                    continue;
                }
                catch(InvalidProgramException e)
                {
                    ObjectPartInventoryItem item = ev.Item;
                    ScriptInstance instance = item.ScriptInstance;
                    /* stop the broken script */
                    m_Log.WarnFormat("Automatically stopped script {0} ({1}) of {2} ({3}) in {4} ({5}) due to program error: {6}\n{7}",
                        item.Name, item.AssetID.ToString(),
                        ev.Part.Name, ev.Part.ID.ToString(),
                        ev.Part.ObjectGroup.Name, ev.Part.ObjectGroup.ID.ToString(),
                        e.Message,
                        e.StackTrace);
                    instance.IsRunning = false;
                    continue;
                }
                finally
                {
                    lock (tc)
                    {
                        tc.CurrentScriptInstance = null;
                    }
                }

                lock (ev)
                {
                    if (ev.HasEventsPending)
                    {
                        pool.m_ScriptTriggerQueue.Enqueue(ev);
                    }
                    else
                    {
                        ev.ThreadPool = null;
                    }
                }
            }

            lock (m_Threads)
            {
                m_Threads.Remove(tc);
                if (m_Threads.Count == 0)
                {
                    m_WaitShutdownEvent.Set();
                }
            }
        }
    }
}
