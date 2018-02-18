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

using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Threading
{
    public class BlockingQueue<T> : Queue<T>
    {
        private readonly object m_Lock = new object();

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

        public new T Peek()
        {
            lock(m_Lock)
            {
                return base.Peek();
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
                while (base.Count == 0)
                {
                    if (!Monitor.Wait(m_Lock, timeout))
                    {
                        throw new TimeoutException();
                    }
                }
                return base.Dequeue();
            }
        }

        public bool TryDequeue(out T value)
        {
            lock(m_Lock)
            {
                if(base.Count != 0)
                {
                    value = base.Dequeue();
                    return true;
                }
            }
            value = default(T);
            return false;
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