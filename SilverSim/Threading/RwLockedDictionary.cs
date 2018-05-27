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
using System.Runtime.Serialization;
using System.Threading;

namespace SilverSim.Threading
{
    public class RwLockedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        protected ReaderWriterLock m_RwLock = new ReaderWriterLock();
        protected Dictionary<TKey, TValue> m_Dictionary;

        [Serializable]
        public class KeyAlreadyExistsException : Exception
        {
            public KeyAlreadyExistsException(string message)
                : base(message)
            {
            }

            public KeyAlreadyExistsException()
            {
            }

            public KeyAlreadyExistsException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected KeyAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            {
            }
        }

        public RwLockedDictionary()
        {
            m_Dictionary = new Dictionary<TKey, TValue>();
        }

        public RwLockedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public RwLockedDictionary(IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(comparer);
        }

        public RwLockedDictionary(int capacity)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(capacity);
        }

        public RwLockedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(dictionary, comparer);
        }

        public RwLockedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new Dictionary<TKey,TValue>(capacity, comparer);
        }

        public bool IsReadOnly => false;
        public int Count => m_RwLock.AcquireReaderLock(() => m_Dictionary.Count);

        public TValue this[TKey key]
        {
            get
            {
                return m_RwLock.AcquireReaderLock(() => m_Dictionary[key]);
            }
            set
            {
                m_RwLock.AcquireWriterLock(() =>
                {
                    m_Dictionary[key] = value;
                });
            }
        }

        public delegate TValue CreateValueDelegate();
        public TValue AddIfNotExists(TKey key, CreateValueDelegate del) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_Dictionary.ContainsKey(key))
            {
                throw new KeyAlreadyExistsException("Key \"" + key.ToString() + "\" already exists");
            }
            TValue res = del();
            m_Dictionary.Add(key, res);
            return res;
        });

        public TValue GetOrAddIfNotExists(TKey key, CreateValueDelegate del) => m_RwLock.AcquireReaderLock(() =>
        {
            TValue value;
            if(m_Dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return m_RwLock.UpgradeToWriterLock(() =>
                {
                    if (m_Dictionary.TryGetValue(key, out value))
                    {
                        return value;
                    }
                    return m_Dictionary[key] = del();
                });
            }
        });

        public void Add(TKey key, TValue value) => m_RwLock.AcquireWriterLock(() =>
        {
            m_Dictionary.Add(key, value);
        });

        public void Add(KeyValuePair<TKey, TValue> kvp) => m_RwLock.AcquireWriterLock(() =>
        {
            m_Dictionary.Add(kvp.Key, kvp.Value);
        });

        public void Clear() => m_RwLock.AcquireWriterLock(() => m_Dictionary.Clear());

        public bool Contains(KeyValuePair<TKey, TValue> kvp) => m_RwLock.AcquireReaderLock(() => m_Dictionary.ContainsKey(kvp.Key));

        public bool Contains(TKey key, TValue value) => m_RwLock.AcquireReaderLock(() => m_Dictionary.ContainsKey(key));

        public bool ContainsKey(TKey key) => m_RwLock.AcquireReaderLock(() => m_Dictionary.ContainsKey(key));

        public bool ContainsValue(TValue value) => m_RwLock.AcquireReaderLock(() =>
        {
            foreach (KeyValuePair<TKey, TValue> kvp in m_Dictionary)
            {
                if (kvp.Value.Equals(value))
                {
                    return true;
                }
            }
            return false;
        });

        public bool Remove(KeyValuePair<TKey, TValue> kvp) => m_RwLock.AcquireWriterLock(() => m_Dictionary.Remove(kvp.Key));

        public bool Remove(TKey key) => m_RwLock.AcquireWriterLock(() => m_Dictionary.Remove(key));

        public bool Remove(TKey key, out TValue val)
        {
            TValue v = default(TValue);
            bool s = m_RwLock.AcquireWriterLock(() =>
            {
                if (m_Dictionary.ContainsKey(key))
                {
                    v = m_Dictionary[key];
                }
                return m_Dictionary.Remove(key);
            });
            val = v;
            return s;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            TValue v = default(TValue);
            bool s = m_RwLock.AcquireReaderLock(() =>
            {
                return m_Dictionary.TryGetValue(key, out v);
            });
            value = v;
            return s;
        }

        public ICollection<TKey> Keys => m_RwLock.AcquireReaderLock(() => new List<TKey>(m_Dictionary.Keys));

        public ICollection<TValue> Values => m_RwLock.AcquireReaderLock(() => new List<TValue>(m_Dictionary.Values));

        public void CopyTo(KeyValuePair<TKey, TValue>[] array,
            int arrayIndex)
        {
            m_RwLock.AcquireReaderLock(() =>
            {
                foreach (KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                {
                    array[arrayIndex++] = kvp;
                }
            });
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => m_RwLock.AcquireReaderLock(() =>
        {
            return (new Dictionary<TKey, TValue>(m_Dictionary)).GetEnumerator();
        });

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TKey[] GetKeyStrings() => m_RwLock.AcquireReaderLock(() =>
        {
            TKey[] __keys = new TKey[m_Dictionary.Count];
            m_Dictionary.Keys.CopyTo(__keys, 0);
            return __keys;
        });

        public delegate bool CheckIfRemove(TValue value);

        public bool RemoveIf(TKey key, CheckIfRemove del) => m_RwLock.AcquireWriterLock(() =>
        {
            if (m_Dictionary.ContainsKey(key) &&
                !del(m_Dictionary[key]))
            {
                return false;
            }
            return m_Dictionary.Remove(key);
        });

        public bool RemoveIf(TKey key, CheckIfRemove del, out KeyValuePair<TKey, TValue> kvp)
        {
            var k = default(KeyValuePair<TKey, TValue>);
            bool s = m_RwLock.AcquireWriterLock(() =>
            {
                if (m_Dictionary.ContainsKey(key))
                {
                    TValue val = m_Dictionary[key];
                    if (!del(val))
                    {
                        return false;
                    }
                    k = new KeyValuePair<TKey, TValue>(key, val);
                }
                return m_Dictionary.Remove(key);
            });
            kvp = k;
            return s;
        }

        public delegate bool FindIfRemove(TKey key, TValue value);
        public bool FindRemoveIf(FindIfRemove del, out KeyValuePair<TKey, TValue> kvpout)
        {
            var kout = default(KeyValuePair<TKey, TValue>);
            bool s = m_RwLock.AcquireReaderLock(() =>
            {
                foreach (KeyValuePair<TKey, TValue> kvp in m_Dictionary)
                {
                    if (!del(kvp.Key, kvp.Value))
                    {
                        continue;
                    }
                    LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                    try
                    {
                        if (m_Dictionary.ContainsKey(kvp.Key))
                        {
                            m_Dictionary.Remove(kvp.Key);
                            kout = kvp;
                            return true;
                        }
                    }
                    finally
                    {
                        m_RwLock.DowngradeFromWriterLock(ref lc);
                    }
                }
                return false;
            });
            kvpout = kout;
            return s;
        }

        public void Remove(IEnumerable<TKey> keys) => m_RwLock.AcquireWriterLock(() =>
        {
            foreach (TKey key in keys)
            {
                m_Dictionary.Remove(key);
            }
        });

        public void Replace(IDictionary<TKey, TValue> dictionary) => m_RwLock.AcquireWriterLock(() =>
        {
            m_Dictionary = new Dictionary<TKey, TValue>(dictionary);
        });

        public delegate bool CheckReplaceDelegate(TValue value);

        public bool AddOrReplaceValueIf(TKey key, TValue value, CheckReplaceDelegate del) => m_RwLock.AcquireWriterLock(() =>
        {
            TValue checkval;
            if (m_Dictionary.TryGetValue(key, out checkval) &&
                !del(checkval))
            {
                return false;
            }
            m_Dictionary[key] = value;
            return true;
        });

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items) => m_RwLock.AcquireWriterLock(() =>
        {
            foreach (KeyValuePair<TKey, TValue> kvp in items)
            {
                m_Dictionary.Add(kvp.Key, kvp.Value);
            }
        });
    }

    public class RwLockedDictionaryAutoAdd<TKey, TValue> : RwLockedDictionary<TKey, TValue>
    {
        private readonly CreateValueDelegate m_AutoAddDelegate;
        public RwLockedDictionaryAutoAdd(CreateValueDelegate autoAddDelegate)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(IDictionary<TKey, TValue> dictionary, CreateValueDelegate autoAddDelegate)
            : base(dictionary)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(IEqualityComparer<TKey> comparer, CreateValueDelegate autoAddDelegate)
            : base(comparer)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(int capacity, CreateValueDelegate autoAddDelegate)
            : base(capacity)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer, CreateValueDelegate autoAddDelegate)
            : base(dictionary, comparer)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public RwLockedDictionaryAutoAdd(int capacity, IEqualityComparer<TKey> comparer, CreateValueDelegate autoAddDelegate)
            : base(capacity, comparer)
        {
            m_AutoAddDelegate = autoAddDelegate;
        }

        public new TValue this[TKey key]
        {
            get
            {
                return m_RwLock.AcquireReaderLock(() =>
                {

                    try
                    {
                        return m_Dictionary[key];
                    }
                    catch (KeyNotFoundException)
                    {
                        return m_RwLock.UpgradeToWriterLock(() => m_Dictionary[key] = m_AutoAddDelegate());
                    }
                });
            }
            set
            {
                m_RwLock.AcquireWriterLock(() =>
                {
                    m_Dictionary[key] = value;
                });
            }
        }
    }
}