// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Threading;
using System.Collections.Generic;

namespace SilverSim.Threading
{
    public class BlockingQueue<T> : Queue<T>
    {
        readonly object m_Lock = new object();

        public BlockingQueue(IEnumerable<T> col)
            : base(col)
        {
        }

        public BlockingQueue(int capacity)
            : base(capacity)
        {
        }

        public BlockingQueue()
        {
        }

        ~BlockingQueue()
        {
            lock(m_Lock)
            {
                base.Clear();
                Monitor.PulseAll(m_Lock);
            }
        }

        public new T Dequeue()
        {
            return Dequeue(Timeout.Infinite);
        }

        public T Dequeue(TimeSpan timeout)
        {
            return Dequeue(timeout.Milliseconds);
        }

        public T Dequeue(int timeout)
        {
            lock(m_Lock)
            {
                while(base.Count == 0)
                {
                    if(!Monitor.Wait(m_Lock, timeout))
                    {
                        throw new TimeoutException();
                    }
                }
                return base.Dequeue();
            }
        }

        public new void Enqueue(T obj)
        {
            lock (m_Lock)
            {
                base.Enqueue(obj);
                Monitor.Pulse(m_Lock);
            }
        }

        public new int Count
        {
            get
            {
                lock (m_Lock)
                {
                    return base.Count;
                }
            }
        }

        public new bool Contains(T obj)
        {
            lock(m_Lock)
            {
                return base.Contains(obj);
            }
        }
    }
}