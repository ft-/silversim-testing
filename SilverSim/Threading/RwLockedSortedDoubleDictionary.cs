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

namespace SilverSim.Threading
{
    public class RwLockedSortedDoubleDictionary<TKey1, TKey2, TValue>
    {
        [Serializable]
        public class ChangeKeyFailedException : Exception
        {
            public ChangeKeyFailedException(string message)
                : base(message)
            {
            }

            public ChangeKeyFailedException() : base()
            {
            }

            public ChangeKeyFailedException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected ChangeKeyFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }
        }

        private readonly SortedDictionary<TKey1, KeyValuePair<TKey2, TValue>> m_Dictionary_K1;
        private readonly SortedDictionary<TKey2, KeyValuePair<TKey1, TValue>> m_Dictionary_K2;
        private readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

        public RwLockedSortedDoubleDictionary()
        {
            m_Dictionary_K1 = new SortedDictionary<TKey1, KeyValuePair<TKey2, TValue>>();
            m_Dictionary_K2 = new SortedDictionary<TKey2, KeyValuePair<TKey1, TValue>>();
        }

        public RwLockedSortedDoubleDictionary(IComparer<TKey1> comparer1, IComparer<TKey2> comparer2)
        {
            m_Dictionary_K1 = new SortedDictionary<TKey1, KeyValuePair<TKey2, TValue>>(comparer1);
            m_Dictionary_K2 = new SortedDictionary<TKey2, KeyValuePair<TKey1, TValue>>(comparer2);
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_Dictionary_K1.ContainsKey(key1))
            {
                if (!m_Dictionary_K2.ContainsKey(key2))
                {
                    throw new ArgumentException("key1 exists in the dictionary but not key2");
                }
            }
            else if (m_Dictionary_K2.ContainsKey(key2) &&
                !m_Dictionary_K1.ContainsKey(key1))
            {
                throw new ArgumentException("key2 exists in the dictionary but not key1");
            }

            m_Dictionary_K1[key1] = new KeyValuePair<TKey2, TValue>(key2, value);
            m_Dictionary_K2[key2] = new KeyValuePair<TKey1, TValue>(key1, value);
        });

        public bool Remove(TKey1 key1, TKey2 key2) => m_RwLock.AcquireWriterLock(() =>
        {
            KeyValuePair<TKey2, TValue> kvp;
            if (m_Dictionary_K1.TryGetValue(key1, out kvp))
            {
                if (!kvp.Key.Equals(key2))
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
            KeyValuePair<TKey2, TValue> kvp;
            if (m_Dictionary_K1.TryGetValue(key1, out kvp))
            {
                m_Dictionary_K1.Remove(key1);
                m_Dictionary_K2.Remove(kvp.Key);
                return true;
            }
            return false;
        });

        public bool Remove(TKey2 key2) => m_RwLock.AcquireWriterLock(() =>
        {
            KeyValuePair<TKey1, TValue> kvp;
            if (m_Dictionary_K2.TryGetValue(key2, out kvp))
            {
                m_Dictionary_K1.Remove(kvp.Key);
                m_Dictionary_K2.Remove(key2);
                return true;
            }
            return false;
        });

        public bool Remove(TKey1 key1, out TValue val)
        {
            TValue v = default(TValue);
            bool s = m_RwLock.AcquireWriterLock(() =>
            {
                KeyValuePair<TKey2, TValue> kvp;
                if (m_Dictionary_K1.TryGetValue(key1, out kvp))
                {
                    m_Dictionary_K1.Remove(key1);
                    m_Dictionary_K2.Remove(kvp.Key);
                    v = kvp.Value;
                    return true;
                }
                return false;
            });
            val = v;
            return s;
        }

        public bool Remove(TKey2 key2, out TValue val)
        {
            TValue v = default(TValue);
            bool s = m_RwLock.AcquireWriterLock(() =>
            {
                KeyValuePair<TKey1, TValue> kvp;
                if (m_Dictionary_K2.TryGetValue(key2, out kvp))
                {
                    m_Dictionary_K1.Remove(kvp.Key);
                    m_Dictionary_K2.Remove(key2);
                    v = kvp.Value;
                    return true;
                }
                return false;
            });
            val = v;
            return s;
        }

        public void Clear() => m_RwLock.AcquireWriterLock(() =>
        {
            m_Dictionary_K1.Clear();
            m_Dictionary_K2.Clear();
        });

        public List<TKey1> Keys1 => m_RwLock.AcquireReaderLock(() => new List<TKey1>(m_Dictionary_K1.Keys));

        public List<TKey2> Keys2 => m_RwLock.AcquireReaderLock(() => new List<TKey2>(m_Dictionary_K2.Keys));

        public int Count => m_RwLock.AcquireReaderLock(() => m_Dictionary_K1.Count);

        public bool ContainsKey(TKey1 key) => m_RwLock.AcquireReaderLock(() => m_Dictionary_K1.ContainsKey(key));

        public bool ContainsKey(TKey2 key) => m_RwLock.AcquireReaderLock(() => m_Dictionary_K2.ContainsKey(key));

        public bool TryGetValue(TKey1 key, out TValue value)
        {
            TValue v = default(TValue);
            bool s = m_RwLock.AcquireReaderLock(() =>
            {
                KeyValuePair<TKey2, TValue> kvp;
                bool success = m_Dictionary_K1.TryGetValue(key, out kvp);
                if (success)
                {
                    v = kvp.Value;
                }
                return success;
            });
            value = v;
            return s;
        }

        public bool TryGetValue(TKey2 key, out TValue value)
        {
            TValue v = default(TValue);
            bool res = m_RwLock.AcquireReaderLock(() =>
            {
                KeyValuePair<TKey1, TValue> kvp;
                bool success = m_Dictionary_K2.TryGetValue(key, out kvp);
                if (success)
                {
                    v = kvp.Value;
                }
                return success;
            });
            value = v;
            return res;
        }

        public TValue this[TKey1 key]
        {
            get
            {
                return m_RwLock.AcquireReaderLock(() => m_Dictionary_K1[key].Value);
            }
            set
            {
                m_RwLock.AcquireWriterLock(() =>
                {
                    var kvp = m_Dictionary_K1[key];
                    m_Dictionary_K2[kvp.Key] = new KeyValuePair<TKey1, TValue>(key, value);
                    m_Dictionary_K1[key] = new KeyValuePair<TKey2, TValue>(kvp.Key, value);
                });
            }
        }

        public TValue this[TKey2 key]
        {
            get
            {
                return m_RwLock.AcquireReaderLock(() => m_Dictionary_K2[key].Value);
            }
            set
            {
                m_RwLock.AcquireWriterLock(() =>
                {
                    var kvp = m_Dictionary_K2[key];
                    m_Dictionary_K2[key] = new KeyValuePair<TKey1, TValue>(kvp.Key, value);
                    m_Dictionary_K1[kvp.Key] = new KeyValuePair<TKey2, TValue>(key, value);
                });
            }
        }

        public void CopyTo(out Dictionary<TKey1, TValue> result)
        {
            var res = new Dictionary<TKey1, TValue>();

            m_RwLock.AcquireReaderLock(() =>
            {
                foreach (var kvp in m_Dictionary_K1)
                {
                    res.Add(kvp.Key, kvp.Value.Value);
                }
            });
            result = res;
        }

        public void CopyTo(out Dictionary<TKey2, TValue> result)
        {
            var res = new Dictionary<TKey2, TValue>();

            m_RwLock.AcquireReaderLock(() =>
            {
                foreach (var kvp in m_Dictionary_K2)
                {
                    res.Add(kvp.Key, kvp.Value.Value);
                }
            });

            result = res;
        }

        public void ChangeKey(TKey1 newKey, TKey1 oldKey)
        {
            m_RwLock.AcquireWriterLock(() =>
            {
                if (m_Dictionary_K1.ContainsKey(newKey))
                {
                    throw new ChangeKeyFailedException("New key already exists: " + newKey.ToString());
                }
                var kvp = m_Dictionary_K1[oldKey];
                m_Dictionary_K1.Remove(oldKey);

                /* re-adjust dictionaries */
                m_Dictionary_K2[kvp.Key] = new KeyValuePair<TKey1, TValue>(newKey, kvp.Value);
                m_Dictionary_K1[newKey] = new KeyValuePair<TKey2, TValue>(kvp.Key, kvp.Value);
            });
        }

        public void ChangeKey(TKey2 newKey, TKey2 oldKey)
        {
            m_RwLock.AcquireWriterLock(() =>
            {
                if (m_Dictionary_K2.ContainsKey(newKey))
                {
                    throw new ChangeKeyFailedException("New key already exists: " + newKey.ToString());
                }
                var kvp = m_Dictionary_K2[oldKey];
                m_Dictionary_K2.Remove(oldKey);

                /* re-adjust dictionaries */
                m_Dictionary_K1[kvp.Key] = new KeyValuePair<TKey2, TValue>(newKey, kvp.Value);
                m_Dictionary_K2[newKey] = new KeyValuePair<TKey1, TValue>(kvp.Key, kvp.Value);
            });
        }

        public List<TValue> Values => m_RwLock.AcquireReaderLock(() =>
        {
            var result = new List<TValue>();
            foreach (var kvp in m_Dictionary_K1.Values)
            {
                result.Add(kvp.Value);
            }

            return result;
        });

        public List<TValue> ValuesByKey1
        {
            get { return Values; }
        }

        public List<TValue> ValuesByKey2 => m_RwLock.AcquireReaderLock(() =>
        {
            var result = new List<TValue>();
            foreach (var kvp in m_Dictionary_K2.Values)
            {
                result.Add(kvp.Value);
            }

            return result;
        });

        public IDictionary<TKey1, TValue> Key1ValuePairs => m_RwLock.AcquireReaderLock(() =>
        {
            var result = new Dictionary<TKey1, TValue>();
            foreach (var kvp in m_Dictionary_K2.Values)
            {
                result.Add(kvp.Key, kvp.Value);
            }
            return result;
        });

        public IDictionary<TKey2, TValue> Key2ValuePairs => m_RwLock.AcquireReaderLock(() =>
        {
            var result = new Dictionary<TKey2, TValue>();
            foreach (var kvp in m_Dictionary_K1.Values)
            {
                result.Add(kvp.Key, kvp.Value);
            }
            return result;
        });
    }
}
