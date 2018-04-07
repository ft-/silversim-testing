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
using System.Runtime.Serialization;
using System.Threading;

namespace SilverSim.Threading
{
    public class RwLockedDoubleDictionary<TKey1, TKey2, TValue>
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

        private readonly Dictionary<TKey1, KeyValuePair<TKey2, TValue>> m_Dictionary_K1;
        private readonly Dictionary<TKey2, KeyValuePair<TKey1, TValue>> m_Dictionary_K2;
        private readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

        public RwLockedDoubleDictionary()
        {
            m_Dictionary_K1 = new Dictionary<TKey1, KeyValuePair<TKey2, TValue>>();
            m_Dictionary_K2 = new Dictionary<TKey2, KeyValuePair<TKey1, TValue>>();
        }

        public RwLockedDoubleDictionary(int capacity)
        {
            m_Dictionary_K1 = new Dictionary<TKey1, KeyValuePair<TKey2, TValue>>(capacity);
            m_Dictionary_K2 = new Dictionary<TKey2, KeyValuePair<TKey1, TValue>>(capacity);
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_Dictionary_K1.ContainsKey(key1))
            {
                throw new DuplicateKey1Exception(string.Format("Key pair \"{0}\",\"{1}\" is duplicated on key 1", key1.ToString(), key2.ToString()));
            }
            if (m_Dictionary_K2.ContainsKey(key2))
            {
                throw new DuplicateKey2Exception(string.Format("Key pair \"{0}\",\"{1}\" is duplicated on key 2", key1.ToString(), key2.ToString()));
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
            var v = default(TValue);
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
            var v = default(TValue);
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

        public int Count => m_RwLock.AcquireReaderLock(() => m_Dictionary_K1.Count);

        public bool ContainsKey(TKey1 key) => m_RwLock.AcquireReaderLock(() => m_Dictionary_K1.ContainsKey(key));

        public bool ContainsKey(TKey2 key) => m_RwLock.AcquireReaderLock(() => m_Dictionary_K2.ContainsKey(key));

        public bool TryGetValue(TKey1 key, out TValue value)
        {
            var v = default(TValue);
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
            var v = default(TValue);
            bool s = m_RwLock.AcquireReaderLock(() =>
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
            return s;
        }

        public bool TryGetValue(TKey1 key, out KeyValuePair<TKey2, TValue> value)
        {
            var v = default(KeyValuePair<TKey2, TValue>);
            bool s = m_RwLock.AcquireReaderLock(() =>
            {
                return m_Dictionary_K1.TryGetValue(key, out v);
            });
            value = v;
            return s;
        }

        public bool TryGetValue(TKey2 key, out KeyValuePair<TKey1, TValue> value)
        {
            var v = default(KeyValuePair<TKey1, TValue>);
            bool s = m_RwLock.AcquireReaderLock(() =>
            {
                return m_Dictionary_K2.TryGetValue(key, out v);
            });
            value = v;
            return s;
        }

        public TValue this[TKey1 key]
        {
            get
            {
                return m_RwLock.AcquireReaderLock(() =>
                {
                    return m_Dictionary_K1[key].Value;
                });
            }
            set
            {
                m_RwLock.AcquireWriterLock(() =>
                {
                    KeyValuePair<TKey2, TValue> kvp = m_Dictionary_K1[key];
                    m_Dictionary_K2[kvp.Key] = new KeyValuePair<TKey1, TValue>(key, value);
                    m_Dictionary_K1[key] = new KeyValuePair<TKey2, TValue>(kvp.Key, value);
                });
            }
        }

        public TValue this[TKey2 key]
        {
            get
            {
                return m_RwLock.AcquireReaderLock(() =>
                {
                    return m_Dictionary_K2[key].Value;
                });
            }
            set
            {
                m_RwLock.AcquireWriterLock(() =>
                {
                    KeyValuePair<TKey1, TValue> kvp = m_Dictionary_K2[key];
                    m_Dictionary_K2[key] = new KeyValuePair<TKey1, TValue>(kvp.Key, value);
                    m_Dictionary_K1[kvp.Key] = new KeyValuePair<TKey2, TValue>(key, value);
                });
            }
        }

        public void CopyTo(out Dictionary<TKey1, TValue> result)
        {
            var r = new Dictionary<TKey1, TValue>();

            m_RwLock.AcquireReaderLock(() =>
            {
                foreach (KeyValuePair<TKey1, KeyValuePair<TKey2, TValue>> kvp in m_Dictionary_K1)
                {
                    r.Add(kvp.Key, kvp.Value.Value);
                }
            });
            result = r;
        }

        public void CopyTo(out Dictionary<TKey2, TValue> result)
        {
            var r = new Dictionary<TKey2, TValue>();

            m_RwLock.AcquireReaderLock(() =>
            {
                foreach (KeyValuePair<TKey2, KeyValuePair<TKey1, TValue>> kvp in m_Dictionary_K2)
                {
                    r.Add(kvp.Key, kvp.Value.Value);
                }
            });
            result = r;
        }

        public void ChangeKey(TKey1 newKey, TKey1 oldKey)
        {
            m_RwLock.AcquireWriterLock(() =>
            {
                if (m_Dictionary_K1.ContainsKey(newKey))
                {
                    throw new ChangeKeyFailedException("New key already exists: " + newKey.ToString());
                }
                KeyValuePair<TKey2, TValue> kvp = m_Dictionary_K1[oldKey];
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
                KeyValuePair<TKey1, TValue> kvp = m_Dictionary_K2[oldKey];
                m_Dictionary_K2.Remove(oldKey);

                /* re-adjust dictionaries */
                m_Dictionary_K1[kvp.Key] = new KeyValuePair<TKey2, TValue>(newKey, kvp.Value);
                m_Dictionary_K2[newKey] = new KeyValuePair<TKey1, TValue>(kvp.Key, kvp.Value);
            });
        }

        public List<TValue> Values => m_RwLock.AcquireReaderLock(() =>
        {
            List<TValue> result = new List<TValue>();
            foreach (KeyValuePair<TKey2, TValue> kvp in m_Dictionary_K1.Values)
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
            foreach (KeyValuePair<TKey1, TValue> kvp in m_Dictionary_K2.Values)
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

        public IList<TKey1> Keys1 => m_RwLock.AcquireReaderLock(() =>
        {
            return new List<TKey1>(m_Dictionary_K1.Keys);
        });

        public IList<TKey2> Keys2 => m_RwLock.AcquireReaderLock(() =>
        {
            return new List<TKey2>(m_Dictionary_K2.Keys);
        });
    }
}
