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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Threading
{
    public class RwLockedList<T> : IList<T>
    {
        private readonly List<T> m_List;
        private readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

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

        public int Count => m_RwLock.AcquireReaderLock(() => m_List.Count);

        public void Clear() => m_RwLock.AcquireWriterLock(() => m_List.Clear());

        public IList<T> GetAndClear() => m_RwLock.AcquireWriterLock(() =>
        {
            IList<T> res = new List<T>(m_List);
            m_List.Clear();
            return res;
        });

        public bool Contains(T value) => m_RwLock.AcquireReaderLock(() => m_List.Contains(value));

        public bool IsReadOnly => false;

        public bool Remove(T value) => m_RwLock.AcquireWriterLock(() => m_List.Remove(value));

        public int IndexOf(T value) => m_RwLock.AcquireReaderLock(() => m_List.IndexOf(value));

        public void RemoveAt(int index) => m_RwLock.AcquireWriterLock(() => m_List.RemoveAt(index));

        public delegate bool RemoveMatchDelegate(T val);

        public T RemoveMatch(RemoveMatchDelegate del) => m_RwLock.AcquireWriterLock(() =>
        {
            foreach (T val in m_List)
            {
                if (del(val))
                {
                    m_List.Remove(val);
                    return val;
                }
            }
            return default(T);
        });

        public T this[int index]
        {
            get
            {
                return m_RwLock.AcquireReaderLock(() => m_List[index]);
            }
            set
            {
                m_RwLock.AcquireWriterLock(() =>
                {
                    m_List[index] = value;
                });
            }
        }

        public void Add(T value) => m_RwLock.AcquireWriterLock(() => m_List.Add(value));

        public void CopyTo(T[] array, int arrayIndex) => m_RwLock.AcquireReaderLock(() => m_List.CopyTo(array, arrayIndex));

        public void Insert(int index, T value) => m_RwLock.AcquireWriterLock(() => m_List.Insert(index, value));

        public IEnumerator<T> GetEnumerator() => m_RwLock.AcquireReaderLock(() => (new List<T>(m_List)).GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [Serializable]
        public class ValueAlreadyExistsException : Exception
        {
        }

        public void AddIfNotExists(T val) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_List.Contains(val))
            {
                throw new ValueAlreadyExistsException();
            }
            m_List.Add(val);
        });

        public List<T> FindAll(Predicate<T> match) => m_RwLock.AcquireReaderLock(() => m_List.FindAll(match));

        public T Find(Predicate<T> match) => m_RwLock.AcquireReaderLock(() => m_List.Find(match));
    }
}