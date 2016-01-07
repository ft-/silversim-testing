// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Threading
{
    public class NonblockingQueue<T> : Queue<T>
    {
        readonly object m_Lock = new object();
        public NonblockingQueue(IEnumerable<T> col)
            : base(col)
        {
        }

        public NonblockingQueue(int capacity)
            : base(capacity)
        {
        }

        public NonblockingQueue()
        {
        }

        ~NonblockingQueue()
        {
            lock (m_Lock)
            {
                base.Clear();
            }
        }

        public new T Dequeue()
        {
            lock (m_Lock)
            {
                return base.Dequeue();
            }
        }

        public bool Dequeue(out T e)
        {
            try
            {
                e = Dequeue();
                return true;
            }
            catch(InvalidOperationException)
            {
                e = default(T);
                return false;
            }
        }

        public new T Peek()
        {
            lock(m_Lock)
            {
                return base.Peek();
            }
        }

        public new void Enqueue(T obj)
        {
            lock (m_Lock)
            {
                base.Enqueue(obj);
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
    }
}