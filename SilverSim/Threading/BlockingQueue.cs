// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Threading;
using System.Collections.Generic;

namespace SilverSim.Threading
{
    public class BlockingQueue<T> : Queue<T>
    {
        public class TimeoutException : Exception
        {
            public TimeoutException()
            {
            }
        }

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
            lock(this)
            {
                base.Clear();
                Monitor.PulseAll(this);
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
            lock(this)
            {
                while(base.Count == 0)
                {
                    if(!Monitor.Wait(this, timeout))
                    {
                        throw new TimeoutException();
                    }
                }
                return base.Dequeue();
            }
        }

        public new void Enqueue(T obj)
        {
            lock (this)
            {
                base.Enqueue(obj);
                Monitor.Pulse(this);
            }
        }

        public new int Count
        {
            get
            {
                lock (this) return base.Count;
            }
        }

        public new bool Contains(T obj)
        {
            lock(this)
            {
                return base.Contains(obj);
            }
        }
    }
}