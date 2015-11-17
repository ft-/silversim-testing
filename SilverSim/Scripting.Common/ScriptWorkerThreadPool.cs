// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Object;
using System;
using System.Threading;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Common
{
    public sealed class ScriptWorkerThreadPool : IScriptWorkerThreadPool
    {
        readonly BlockingQueue<ScriptInstance> m_ScriptTriggerQueue = new BlockingQueue<ScriptInstance>();
        private int m_MinimumThreads = 2;
        private int m_MaximumThreads = 150;
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

        public ScriptWorkerThreadPool(int minimumThreads, int maximumThreads)
        {
            MinimumThreads = minimumThreads;
            MaximumThreads = maximumThreads;

            for (int threadCount = 0; threadCount < m_MinimumThreads; ++threadCount)
            {
                ScriptThreadContext tc = new ScriptThreadContext();
                tc.ScriptThread = new Thread(ThreadMain);
                tc.ScriptThread.Name = "Script Worker";
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
            m_ScriptTriggerQueue.Enqueue(i);
            int threadsCount = m_Threads.Count;
            if(m_ScriptTriggerQueue.Count > threadsCount && threadsCount < m_MaximumThreads)
            {
                lock (m_Threads)
                {
                    try
                    {
                        ScriptThreadContext tc = new ScriptThreadContext();
                        tc.ScriptThread = new Thread(ThreadMain);
                        tc.ThreadPool = this;
                        tc.ScriptThread.Name = "Script Worker";
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
                        if (m_Threads.Count > m_MinimumThreads)
                        {
                            m_Threads.Remove(tc);
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
                    lock(tc)
                    {
                        tc.CurrentScriptInstance = null;
                    }
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
                }
                catch(ScriptAbortException)
                {
                    ObjectPartInventoryItem item = ev.Item;
                    ScriptInstance instance = item.ScriptInstance;
                    instance.AbortBegin();
                    instance.Remove();
                    item.ScriptInstance = null;
                    ScriptLoader.Remove(item.AssetID, instance);
                }

                if (ev.HasEventsPending)
                {
                    pool.m_ScriptTriggerQueue.Enqueue(ev);
                }
            }
        }

    }
}
