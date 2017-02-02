// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Threading
{
    public static class ThreadManager
    {
        static readonly RwLockedList<Thread> m_Threads = new RwLockedList<Thread>();

        public static IList<Thread> Threads
        {
            get
            {
                return new List<Thread>(m_Threads);
            }
        }

        class ThreadStartContext
        {
            internal Thread Thread;
            ThreadStart m_Start;
            ParameterizedThreadStart m_ParameterizedStart;

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
                finally
                {
                    m_Threads.Remove(Thread);
                }
            }
        }
        public static Thread CreateThread(ThreadStart start)
        {
            ThreadStartContext tsc = new ThreadStartContext(start);
            Thread t = new Thread(tsc.StartThread);
            tsc.Thread = t;
            return t;
        }

        public static Thread CreateThread(ParameterizedThreadStart start)
        {
            ThreadStartContext tsc = new ThreadStartContext(start);
            Thread t = new Thread(tsc.StartThreadParameterized);
            tsc.Thread = t;
            return t;
        }

        public static Thread CreateThread(ThreadStart start, int maxStackSize)
        {
            ThreadStartContext tsc = new ThreadStartContext(start);
            Thread t = new Thread(tsc.StartThread, maxStackSize);
            tsc.Thread = t;
            return t;
        }

        public static Thread CreateThread(ParameterizedThreadStart start, int maxStackSize)
        {
            ThreadStartContext tsc = new ThreadStartContext(start);
            Thread t = new Thread(tsc.StartThreadParameterized, maxStackSize);
            tsc.Thread = t;
            return t;
        }

    }
}
