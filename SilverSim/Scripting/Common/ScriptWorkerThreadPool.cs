/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Scene.Types.Script;
using System;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scripting.Common
{
    public sealed class ScriptWorkerThreadPool : IScriptWorkerThreadPool
    {
        private BlockingQueue<ScriptInstance> m_ScriptTriggerQueue = new BlockingQueue<ScriptInstance>();
        private int m_MinimumThreads = 2;
        private int m_MaximumThreads = 150;
        public class ScriptThreadContext
        {
            public ScriptInstance CurrentScriptInstance = null;
            public Thread ScriptThread;
            public ScriptWorkerThreadPool ThreadPool;

            public ScriptThreadContext()
            {

            }
        }
        private RwLockedList<ScriptThreadContext> m_Threads = new RwLockedList<ScriptThreadContext>();

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
                ScriptThreadContext tc = new ScriptThreadContext();
                tc.ScriptThread = new Thread(ThreadMain);
                tc.ScriptThread.Name = "Script Worker";
                tc.ThreadPool = this;
                tc.ScriptThread.Start(tc);
                m_Threads.Add(tc);
            }
        }

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

        private void ThreadMain(object obj)
        {
            ScriptThreadContext tc = (ScriptThreadContext)obj;
            ScriptWorkerThreadPool pool = (ScriptWorkerThreadPool) obj;
            ScriptInstance ev;
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
                            m_Threads.Remove(tc);
                            return;
                        }
                    }
                    return;
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
                }

                if (ev.HasEventsPending)
                {
                    pool.m_ScriptTriggerQueue.Enqueue(ev);
                }
            }
        }

    }
}
