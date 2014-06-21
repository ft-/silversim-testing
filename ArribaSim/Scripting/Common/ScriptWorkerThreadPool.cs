using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ArribaSim.Scene.Types.Script;
using ArribaSim.Scene.Types.Script.Events;
using ThreadedClasses;
namespace ArribaSim.Scripting.Common
{
    public sealed class ScriptWorkerThreadPool
    {
        private BlockingQueue<IScriptInstance> m_ScriptTriggerQueue = new BlockingQueue<IScriptInstance>();
        private RwLockedList<Thread> m_Threads = new RwLockedList<Thread>();
        private int m_MinimumThreads = 2;
        private int m_MaximumThreads = 150;

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
                    throw new ArgumentException();
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
                if(value < m_MinimumThreads)
                {
                    m_MaximumThreads = m_MinimumThreads;
                }
                else
                {
                    m_MaximumThreads = value;
                }
            }
        }

        public ScriptWorkerThreadPool(int minimumThreads, int maximumThreads)
        {
            MinimumThreads = minimumThreads;
            MaximumThreads = maximumThreads;

            for (int threadCount = 0; threadCount < m_MinimumThreads; ++threadCount)
            { 
                Thread t = new Thread(ThreadMain);
                t.Start(this);
                m_Threads.Add(t);
            }
        }

        public void PostScript(IScriptInstance i)
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
                        Thread t = new Thread(ThreadMain);
                        t.Start(this);
                        m_Threads.Add(t);
                    }
                    catch
                    {
                        /* do not fail when we could not add a thread */
                    }
                }
            }
        }

        private void ThreadMain(object obj)
        {
            ScriptWorkerThreadPool pool = (ScriptWorkerThreadPool) obj;
            IScriptInstance ev;
            while (true)
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
                            m_Threads.Remove(Thread.CurrentThread);
                            return;
                        }
                    }
                    return;
                }

                ev.ProcessEvent();

                if (ev.HasEventsPending)
                {
                    pool.m_ScriptTriggerQueue.Enqueue(ev);
                }
            }
        }

    }
}
