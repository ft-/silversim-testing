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
using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;

namespace SilverSim.Scripting.Common
{
    public sealed class ScriptWorkerThreadPool : IScriptWorkerThreadPool
    {
        private sealed class FactoryClass : IScriptWorkerThreadPoolFactory
        {
            public IScriptWorkerThreadPool InstantiateThreadPool(UUID sceneID)
            {
                return new ScriptWorkerThreadPool(50, 150, sceneID);
            }
        }

        public static readonly IScriptWorkerThreadPoolFactory Factory = new FactoryClass();

        private static readonly ILog m_Log = LogManager.GetLogger("SCRIPT WORKER THREAD POOL");

        private readonly BlockingQueue<ScriptInstance> m_ScriptTriggerQueue = new BlockingQueue<ScriptInstance>();
        private readonly ManualResetEvent m_WaitShutdownEvent = new ManualResetEvent(false);
        private int m_MinimumThreads = 2;
        private int m_MaximumThreads = 150;
        private readonly UUID m_SceneID;
        private bool m_ShutdownThreads;
        private readonly System.Timers.Timer m_FrameTimer = new System.Timers.Timer(1 / 10.0);
        private static readonly TimeProvider TimeSource = TimeProvider.StopWatch;

        private int m_ScriptEventCounter;
        private int m_LastScriptEventCounter;
        private long m_LastScriptEventTickCount;
        private bool m_FirstEventEps = true;
        private long m_ScriptRuntimeAccumulatingTickCount;
        private long m_LastScriptRuntimeAccumulatingTickCount;

        public void IncrementScriptEventCounter()
        {
            Interlocked.Increment(ref m_ScriptEventCounter);
        }

        public int ScriptEventCounter => m_ScriptEventCounter;

        public double ScriptEventsPerSec
        {
            get;
            private set;
        }

        public double ScriptTimeMsPerSec
        {
            get;
            private set;
        }

        public class ScriptThreadContext
        {
            public ScriptInstance CurrentScriptInstance;
            public Thread ScriptThread;
            public ScriptWorkerThreadPool ThreadPool;
            public bool IsExecuting;
            public long ExecutionStartTickCount;
        }

        private readonly RwLockedList<ScriptThreadContext> m_Threads = new RwLockedList<ScriptThreadContext>();
        private RwLockedDictionary<uint /* localids */, ScriptReportData> m_TopScripts = new RwLockedDictionary<uint, ScriptReportData>();
        private RwLockedDictionary<uint /* localids */, ScriptReportData> m_LastTopScripts = new RwLockedDictionary<uint, ScriptReportData>();

        public RwLockedDictionary<uint /* localids */, ScriptReportData> GetExecutionTimes()
        {
            return m_LastTopScripts;
        }

        public void FrameTimer(object o, ElapsedEventArgs args)
        {
            RwLockedDictionary<uint /* localids */, ScriptReportData> oldTopScripts = m_TopScripts;
            m_TopScripts = new RwLockedDictionary<uint, ScriptReportData>();
            m_LastTopScripts = oldTopScripts;
            long tickCount = TimeSource.TickCount;
            if (m_FirstEventEps)
            {
                m_LastScriptEventTickCount = tickCount;
                m_LastScriptEventCounter = m_ScriptEventCounter;
                m_FirstEventEps = false;
            }
            else if (TimeSource.TicksElapsed(tickCount, m_LastScriptEventTickCount) >= TimeSource.Frequency)
            {
                long deltaticks = TimeSource.TicksElapsed(tickCount, m_LastScriptEventTickCount);
                int newEvents = m_ScriptEventCounter;
                ScriptEventsPerSec = TimeSource.NormalizedToEventsPerSeconds(newEvents - m_LastScriptEventCounter, deltaticks);
                m_LastScriptEventTickCount = tickCount;
                m_LastScriptEventCounter = newEvents;
                long newaccum = Interlocked.Read(ref m_ScriptRuntimeAccumulatingTickCount);
                long totalscriptexec = newaccum - m_LastScriptRuntimeAccumulatingTickCount;
                m_LastScriptRuntimeAccumulatingTickCount = newaccum;

                /* add execution times of actually running scripts */
                foreach(ScriptThreadContext tc in m_Threads)
                {
                    if(tc.IsExecuting)
                    {
                        long diff = Math.Min(TimeSource.TicksElapsed(tickCount, tc.ExecutionStartTickCount), deltaticks);
                        totalscriptexec += diff;
                    }
                }
                ScriptTimeMsPerSec = TimeSource.TicksToMsecs(totalscriptexec);
            }
        }

        public int MinimumThreads
        {
            get { return m_MinimumThreads; }
            set
            {
                if (value < 2)
                {
                    throw new ArgumentException("value");
                }
                if (value > m_MaximumThreads)
                {
                    m_MaximumThreads = value;
                }
                m_MinimumThreads = value;
            }
        }

        public int MaximumThreads
        {
            get { return m_MaximumThreads; }
            set { m_MaximumThreads = value < m_MinimumThreads ? m_MinimumThreads : value; }
        }

        public ScriptWorkerThreadPool(int minimumThreads, int maximumThreads, UUID sceneID)
        {
            MinimumThreads = minimumThreads;
            MaximumThreads = maximumThreads;
            m_SceneID = sceneID;

            m_Log.InfoFormat("Starting {0} minimum threads for {1}", minimumThreads, m_SceneID.ToString());
            for (int threadCount = 0; threadCount < m_MinimumThreads; ++threadCount)
            {
                var tc = new ScriptThreadContext
                {
                    ScriptThread = ThreadManager.CreateThread(ThreadMain),
                    ThreadPool = this
                };
                tc.ScriptThread.Name = "Script Worker: " + m_SceneID.ToString();
                tc.ScriptThread.IsBackground = true;
                tc.ScriptThread.Start(tc);
                m_Threads.Add(tc);
            }
            m_FrameTimer.Elapsed += FrameTimer;
            m_FrameTimer.Start();
        }

        ~ScriptWorkerThreadPool()
        {
            m_WaitShutdownEvent.Dispose();
        }

        public void PostScript(ScriptInstance i)
        {
            /* Do not enqueue the already queued script */
            if(m_ScriptTriggerQueue.Contains(i))
            {
                return;
            }

            bool enqueued = false;
            if (i.ThreadPool == null)
            {
                m_ScriptTriggerQueue.Enqueue(i);
                enqueued = true;
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
                            var tc = new ScriptThreadContext
                            {
                                ScriptThread = ThreadManager.CreateThread(ThreadMain),
                                ThreadPool = this
                            };
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
            foreach(ScriptThreadContext tc in m_Threads)
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
            }
        }

        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        public void Sleep(TimeSpan timespan)
        {
            Thread.Sleep(timespan);
        }

        public void Shutdown()
        {
            m_FrameTimer.Stop();
            m_ShutdownThreads = true;
            if (m_Threads.Count != 0)
            {
                m_Log.InfoFormat("Waiting for script shutdown of region {0}", m_SceneID.ToString());
                if (!m_WaitShutdownEvent.WaitOne(30000))
                {
                    /* we have to abort threads */
                    m_Log.InfoFormat("Killing blocked instances of region {0}", m_SceneID.ToString());
                    foreach(var tc in m_Threads)
                    {
                        lock(tc)
                        {
                            var instance = tc.CurrentScriptInstance;
                            if (instance != null)
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

        int m_ExecutingScripts;

        public int ExecutingScripts => m_ExecutingScripts;

        public IList<ScriptInfo> ExecutingScriptsList
        {
            get
            {
                var list = new List<ScriptInfo>();

                foreach(var tc in m_Threads)
                {
                    lock(tc)
                    {
                        var instance = tc.CurrentScriptInstance;
                        if(instance != null)
                        {
                            try
                            {
                                var scriptInfo = new ScriptInfo
                                {
                                    ItemID = instance.Item.ID,
                                    PartID = instance.Part.ID,
                                    PartName = instance.Part.Name,
                                    ItemName = instance.Part.Name,
                                    LinkNumber = instance.Part.LinkNumber,
                                    ObjectID = instance.Part.ObjectGroup.ID,
                                    ObjectName = instance.Part.ObjectGroup.Name,
                                    AssetID = instance.Item.AssetID
                                };
                                list.Add(scriptInfo);
                            }
                            catch
                            {
                                /* ignore */
                            }
                        }
                    }
                }

                return list;
            }
        }

        private void ThreadMain(object obj)
        {
            var tc = (ScriptThreadContext)obj;
            var pool = tc.ThreadPool;
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

                long executionStart = TimeSource.TickCount;
                tc.ExecutionStartTickCount = executionStart;
                tc.IsExecuting = true;
                ObjectPartInventoryItem item = ev.Item;
                try
                {
                    Interlocked.Increment(ref m_ExecutingScripts);
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
                    try
                    {
                        item.ScriptInstance = null;
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();
                    }
                    catch (Exception e)
                    {
                        m_Log.WarnFormat("Exception at script removal {0} ({1}): {2}\n{3}",
                            item.Name, item.AssetID.ToString(),
                            e.Message,
                            e.StackTrace);
                    }
                    ScriptLoader.Remove(item.AssetID, ev);
                    continue;
                }
                catch(ScriptAbortException)
                {
                    ev.AbortBegin();
                    try
                    {
                        item.ScriptInstance = null;
                    }
                    catch(ThreadAbortException)
                    {
                        Thread.ResetAbort();
                    }
                    catch (Exception e)
                    {
                        m_Log.WarnFormat("Exception at script removal {0} ({1}): {2}\n{3}",
                            item.Name, item.AssetID.ToString(),
                            e.Message,
                            e.StackTrace);
                    }
                    ScriptLoader.Remove(item.AssetID, ev);
                    continue;
                }
                catch(InvalidProgramException e)
                {
                    var instance = item.ScriptInstance;
                    /* stop the broken script */
                    m_Log.WarnFormat("Automatically stopped script {0} ({1}) of {2} ({3}) in {4} ({5}) due to program error: {6}\n{7}",
                        item.Name, item.AssetID.ToString(),
                        ev.Part?.Name ?? "?", ev.Part?.ID.ToString() ?? "?",
                        ev.Part?.ObjectGroup?.Name ?? "?", ev.Part?.ObjectGroup?.ID.ToString() ?? "?",
                        e.Message,
                        e.StackTrace);
                    instance.IsRunning = false;
                    continue;
                }
                catch(Exception e)
                {
                    var instance = item.ScriptInstance;
                    m_Log.WarnFormat("Exception at script {0} ({1}) of {2} ({3}) in {4} ({5}) due to program error: {6}\n{7}",
                        item.Name, item.AssetID.ToString(),
                        ev.Part?.Name ?? "?", ev.Part?.ID.ToString() ?? "?",
                        ev.Part?.ObjectGroup?.Name, ev.Part?.ObjectGroup?.ID.ToString() ?? "?",
                        e.Message,
                        e.StackTrace);
                }
                finally
                {
                    Interlocked.Decrement(ref m_ExecutingScripts);
                    tc.IsExecuting = false;
                    uint localId;
                    lock (tc)
                    {
                        try
                        {
                            localId = tc.CurrentScriptInstance.Part.LocalID[m_SceneID];
                            executionStart = TimeSource.TicksElapsed(TimeSource.TickCount, executionStart);
                            if (executionStart > 0)
                            {
                                RwLockedDictionary<uint, ScriptReportData> execTime = m_TopScripts;
                                ScriptReportData prevexectime;
                                if(!execTime.TryGetValue(localId, out prevexectime))
                                {
                                    prevexectime = new ScriptReportData();
                                    execTime.Add(localId, prevexectime);
                                }
                                prevexectime.AddScore(TimeSource.TicksToMsecs(executionStart));
                                Interlocked.Add(ref m_ScriptRuntimeAccumulatingTickCount, executionStart);
                            }
                        }
                        catch
                        {
                            /* ignore it here */
                        }
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
