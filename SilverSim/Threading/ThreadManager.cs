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
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Threading
{
    public static class ThreadManager
    {
        private static readonly ILog m_Log = LogManager.GetLogger("THREAD MANAGER");
        private static readonly RwLockedList<Thread> m_Threads = new RwLockedList<Thread>();

        public static IList<Thread> Threads => new List<Thread>(m_Threads);

        private class ThreadStartContext
        {
            internal Thread Thread;
            private readonly ThreadStart m_Start;
            private readonly ParameterizedThreadStart m_ParameterizedStart;

            public ThreadStartContext(ThreadStart start)
            {
                m_Start = start;
            }

            public ThreadStartContext(ParameterizedThreadStart start)
            {
                m_ParameterizedStart = start;
            }

            public void StartThread()
            {
                try
                {
                    m_Threads.Add(Thread);
                    m_Start();
                }
                catch(Exception e)
                {
                    m_Log.Error($"UNCAUGHT EXCEPTION in {Thread.Name}: {e.GetType().FullName}: {e.Message}\n{e.Data}", e);
                }
                finally
                {
                    m_Threads.Remove(Thread);
                }
            }

            public void StartThreadParameterized(object o)
            {
                try
                {
                    m_Threads.Add(Thread);
                    m_ParameterizedStart(o);
                }
                catch(ThreadAbortException)
                {
                    /* do not log this one */
                }
                catch (Exception e)
                {
                    m_Log.Error($"UNCAUGHT EXCEPTION in {Thread.Name}: {e.GetType().FullName}: {e.Message}\n{e.Data}", e);
                }
                finally
                {
                    m_Threads.Remove(Thread);
                }
            }
        }

        public static Thread CreateThread(ThreadStart start)
        {
            var tsc = new ThreadStartContext(start);
            var t = new Thread(tsc.StartThread);
            tsc.Thread = t;
            return t;
        }

        public static Thread CreateThread(ParameterizedThreadStart start)
        {
            var tsc = new ThreadStartContext(start);
            var t = new Thread(tsc.StartThreadParameterized);
            tsc.Thread = t;
            return t;
        }

        public static Thread CreateThread(ThreadStart start, int maxStackSize)
        {
            var tsc = new ThreadStartContext(start);
            var t = new Thread(tsc.StartThread, maxStackSize);
            tsc.Thread = t;
            return t;
        }

        public static Thread CreateThread(ParameterizedThreadStart start, int maxStackSize)
        {
            var tsc = new ThreadStartContext(start);
            var t = new Thread(tsc.StartThreadParameterized, maxStackSize);
            tsc.Thread = t;
            return t;
        }
    }
}
