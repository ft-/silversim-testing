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
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SilverSim.Threading
{
    public class RwLockedBiDiMappingDictionary<TKey1, TKey2>
    {
        [Serializable]
        public class ChangeKeyFailedException : Exception
        {
            public ChangeKeyFailedException(string message)
                : base(message)
            {
            }

            public ChangeKeyFailedException()
            {
            }

            public ChangeKeyFailedException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected ChangeKeyFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            {
            }
        }

        [Serializable]
        public class DuplicateKey1Exception : ArgumentException
        {
            public DuplicateKey1Exception()
            {
            }

            public DuplicateKey1Exception(string message) : base(message)
            {
            }

            public DuplicateKey1Exception(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected DuplicateKey1Exception(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        [Serializable]
        public class DuplicateKey2Exception : ArgumentException
        {
            public DuplicateKey2Exception()
            {
            }

            public DuplicateKey2Exception(string message) : base(message)
            {
            }

            public DuplicateKey2Exception(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected DuplicateKey2Exception(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        private readonly Dictionary<TKey1, TKey2> m_Dictionary_K1;
        private readonly Dictionary<TKey2, TKey1> m_Dictionary_K2;
        private readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

        public RwLockedBiDiMappingDictionary()
        {
            m_Dictionary_K1 = new Dictionary<TKey1, TKey2>();
            m_Dictionary_K2 = new Dictionary<TKey2, TKey1>();
        }

        public RwLockedBiDiMappingDictionary(int capacity)
        {
            m_Dictionary_K1 = new Dictionary<TKey1, TKey2>(capacity);
            m_Dictionary_K2 = new Dictionary<TKey2, TKey1>(capacity);
        }

        public void Add(TKey1 key1, TKey2 key2) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_Dictionary_K1.ContainsKey(key1))
            {
                throw new DuplicateKey1Exception(string.Format("Key pair \"{0}\",\"{1}\" is duplicated on key 1", key1.ToString(), key2.ToString()));
            }
            if (m_Dictionary_K2.ContainsKey(key2))
            {
                throw new DuplicateKey2Exception(string.Format("Key pair \"{0}\",\"{1}\" is duplicated on key 2", key1.ToString(), key2.ToString()));
            }

            m_Dictionary_K1[key1] = key2;
            m_Dictionary_K2[key2] = key1;
        });

        public bool Remove(TKey1 key1, TKey2 key2) => m_RwLock.AcquireWriterLock(() =>
        {
            TKey2 kvp;
            if (m_Dictionary_K1.TryGetValue(key1, out kvp))
            {
                if (!kvp.Equals(key2))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            m_Dictionary_K1.Remove(key1);
            m_Dictionary_K2.Remove(key2);
            return true;
        });

        public bool Remove(TKey1 key1) => m_RwLock.AcquireWriterLock(() =>
        {
            TKey2 kvp;
            if (m_Dictionary_K1.TryGetValue(key1, out kvp))
            {
                m_Dictionary_K1.Remove(key1);
                m_Dictionary_K2.Remove(kvp);
                return true;
            }
            return false;
        });

        public bool Remove(TKey1 key1, out TKey2 key2)
        {
            var rkey2 = default(TKey2);
            bool res = m_RwLock.AcquireWriterLock(() =>
            {
                if (m_Dictionary_K1.TryGetValue(key1, out rkey2))
                {
                    m_Dictionary_K1.Remove(key1);
                    m_Dictionary_K2.Remove(rkey2);
                    return true;
                }
                return false;
            });
            key2 = rkey2;
            return res;
        }

        public bool Remove(TKey2 key2) => m_RwLock.AcquireWriterLock(() =>
        {
            TKey1 kvp;
            if (m_Dictionary_K2.TryGetValue(key2, out kvp))
            {
                m_Dictionary_K1.Remove(kvp);
                m_Dictionary_K2.Remove(key2);
                return true;
            }
            return false;
        });

        public bool Remove(TKey2 key2, out TKey1 key1)
        {
            var rkey1 = default(TKey1);
            bool res = m_RwLock.AcquireWriterLock(() =>
            {
                if (m_Dictionary_K2.TryGetValue(key2, out rkey1))
                {
                    m_Dictionary_K1.Remove(rkey1);
                    m_Dictionary_K2.Remove(key2);
                    return true;
                }
                return false;
            });
            key1 = rkey1;
            return res;
        }

        public void Clear() => m_RwLock.AcquireWriterLock(() =>
        {
            m_Dictionary_K1.Clear();
            m_Dictionary_K2.Clear();
        });

        public int Count => m_RwLock.AcquireReaderLock(() => m_Dictionary_K1.Count);

        public bool ContainsKey(TKey1 key) => m_RwLock.AcquireReaderLock(() => m_Dictionary_K1.ContainsKey(key));

        public bool ContainsKey(TKey2 key) => m_RwLock.AcquireReaderLock(() => m_Dictionary_K2.ContainsKey(key));

        public bool TryGetValue(TKey1 key, out TKey2 value)
        {
            TKey2 k = default(TKey2);
            bool s = m_RwLock.AcquireReaderLock(() => m_Dictionary_K1.TryGetValue(key, out k));
            value = k;
            return s;
        }

        public bool TryGetValue(TKey2 key, out TKey1 value)
        {
            TKey1 k = default(TKey1);
            bool s = m_RwLock.AcquireReaderLock(() => m_Dictionary_K2.TryGetValue(key, out k));
            value = k;
            return s;
        }

        public TKey2 this[TKey1 key] => m_RwLock.AcquireReaderLock(() => m_Dictionary_K1[key]);

        public TKey1 this[TKey2 key] => m_RwLock.AcquireReaderLock(() => m_Dictionary_K2[key]);

        public void CopyTo(out Dictionary<TKey1, TKey2> result)
        {
            result = m_RwLock.AcquireReaderLock(() =>
            {
                var r = new Dictionary<TKey1, TKey2>();
                foreach (var kvp in m_Dictionary_K1)
                {
                    r.Add(kvp.Key, kvp.Value);
                }
                return r;
            });
        }

        public void ChangeKey(TKey1 newKey, TKey1 oldKey) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_Dictionary_K1.ContainsKey(newKey))
            {
                throw new ChangeKeyFailedException("New key already exists: " + newKey.ToString());
            }
            var kvp = m_Dictionary_K1[oldKey];
            m_Dictionary_K1.Remove(oldKey);

            /* re-adjust dictionaries */
            m_Dictionary_K2[kvp] = newKey;
            m_Dictionary_K1[newKey] = kvp;
        });

        public void ChangeKey(TKey2 newKey, TKey2 oldKey) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_Dictionary_K2.ContainsKey(newKey))
            {
                throw new ChangeKeyFailedException("New key already exists: " + newKey.ToString());
            }
            TKey1 kvp = m_Dictionary_K2[oldKey];
            m_Dictionary_K2.Remove(oldKey);

            /* re-adjust dictionaries */
            m_Dictionary_K1[kvp] = newKey;
            m_Dictionary_K2[newKey] = kvp;
        });

        public List<TKey1> Keys1 => m_RwLock.AcquireReaderLock(() => new List<TKey1>(m_Dictionary_K1.Keys));

        public List<TKey2> Keys2 => m_RwLock.AcquireReaderLock(() => new List<TKey2>(m_Dictionary_K2.Keys));
    }
}
