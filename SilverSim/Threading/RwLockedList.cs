// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Threading
{
    public class RwLockedList<T> : IList<T>
    {
        readonly List<T> m_List;
        readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

        public RwLockedList()
        {
            m_List = new List<T>();
        }

        public RwLockedList(IEnumerable<T> collection)
        {
            m_List = new List<T>(collection);
        }

        public RwLockedList(int capacity)
        {
            m_List = new List<T>(capacity);
        }

        public int Count
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_List.Count;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public void Clear()
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.Clear();
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public bool Contains(T value)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.Contains(value);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                return m_List.Remove(value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public int IndexOf(T value)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.IndexOf(value);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void RemoveAt(int index)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.RemoveAt(index);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public delegate bool RemoveMatchDelegate(T val);

        public T RemoveMatch(RemoveMatchDelegate del)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                foreach(T val in m_List)
                {
                    if(del(val))
                    {
                        m_List.Remove(val);
                        return val;
                    }
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
            return default(T);
        }

        public T this[int index]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_List[index];
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_RwLock.AcquireWriterLock(-1);
                try
                {
                    m_List[index] = value;
                }
                finally
                {
                    m_RwLock.ReleaseWriterLock();
                }
            }
        }

        public void Add(T value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.Add(value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                m_List.CopyTo(array, arrayIndex);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void Insert(int index, T value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.Insert(index, value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return (new List<T>(m_List)).GetEnumerator();
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /* support for non-copy enumeration */
        public void ForEach(Action<T> action)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (T val in m_List)
                {
                    action(val);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public class ValueAlreadyExistsException : Exception
        {
            public ValueAlreadyExistsException()
            {

            }
        }

        public void AddIfNotExists(T val)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if(m_List.Contains(val))
                {
                    throw new ValueAlreadyExistsException();
                }
                m_List.Add(val);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public List<T> FindAll(Predicate<T> match)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.FindAll(match);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public T Find(Predicate<T> match)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.Find(match);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }
    }
}