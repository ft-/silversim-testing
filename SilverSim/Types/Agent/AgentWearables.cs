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

using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Types.Agent
{
    public class AgentWearables
    {
        public struct WearableInfo
        {
            public UUID ItemID;
            public UUID AssetID;

            public WearableInfo(UUID itemID, UUID assetID)
            {
                ItemID = itemID;
                AssetID = assetID;
            }
        }

        readonly ReaderWriterLock m_WearablesUpdateLock = new ReaderWriterLock();
        readonly Dictionary<WearableType, List<WearableInfo>> m_Wearables = new Dictionary<WearableType, List<WearableInfo>>();

        public AgentWearables()
        {
            for(var type = WearableType.Shape; type < WearableType.NumWearables; ++type)
            {
                m_Wearables[type] = new List<WearableInfo>();
            }
        }

        #region Wearable type accessor
        public List<WearableInfo> this[WearableType type]
        {
            get
            {
                m_WearablesUpdateLock.AcquireReaderLock(-1);
                try
                {
                    /* do not give access to our internal data via references */
                    List<WearableInfo> s = m_Wearables[type];
                    var l = new List<WearableInfo>();
                    foreach(var i in s)
                    {
                        l.Add(i);
                    }
                    return l;
                }
                finally
                {
                    m_WearablesUpdateLock.ReleaseReaderLock();
                }
            }

            set
            {
                var nl = new List<WearableInfo>();
                if(value.Count > 5)
                {
                    throw new ArgumentException("Too many elements in list");
                }
                /* do not give access to our internal data */
                foreach(var wi in value)
                {
                    nl.Add(wi);
                }
                if (nl.Count > 5)
                {
                    throw new ArgumentException("Too many elements in list");
                }
                m_WearablesUpdateLock.AcquireWriterLock(-1);
                try
                {
                    m_Wearables[type] = nl;
                }
                finally
                {
                    m_WearablesUpdateLock.ReleaseWriterLock();
                }
            }
        }
        #endregion

        #region Wearable, index accessor
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public WearableInfo this[WearableType type, uint index]
        {
            get
            {
                if(index >= 5)
                {
                    throw new KeyNotFoundException();
                }
                m_WearablesUpdateLock.AcquireReaderLock(-1);
                try
                {
                    return m_Wearables[type][(int)index];
                }
                finally
                {
                    m_WearablesUpdateLock.ReleaseReaderLock();
                }
            }

            set
            {
                m_WearablesUpdateLock.AcquireWriterLock(-1);
                try
                {
                    var wearableList = m_Wearables[type];
                    if (wearableList.Count <= index)
                    {
                        if(index >= 5)
                        {
                            throw new KeyNotFoundException();
                        }
                        wearableList.Add(value);
                    }
                    else
                    {
                        wearableList[(int)index] = value;
                    }
                }
                finally
                {
                    m_WearablesUpdateLock.ReleaseWriterLock();
                }
            }
        }
        #endregion

        #region Replacement accessor
        public static implicit operator Dictionary<WearableType, List<WearableInfo>>(AgentWearables aw) => aw.All;

        public Dictionary<WearableType, List<WearableInfo>> All
        {
            get
            {
                m_WearablesUpdateLock.AcquireReaderLock(-1);
                try
                {
                    var od = new Dictionary<WearableType, List<WearableInfo>>();
                    foreach(var kvp in m_Wearables)
                    {
                        od.Add(kvp.Key, new List<WearableInfo>(kvp.Value));
                    }
                    return od;
                }
                finally
                {
                    m_WearablesUpdateLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_WearablesUpdateLock.AcquireWriterLock(-1);
                try
                {
                    foreach(var lwi in m_Wearables.Values)
                    {
                        lwi.Clear();
                    }

                    foreach (var kvp in value)
                    {
                        if (kvp.Key < WearableType.NumWearables)
                        {
                            m_Wearables[kvp.Key] = new List<WearableInfo>(kvp.Value);
                        }
                    }
                }
                finally
                {
                    m_WearablesUpdateLock.ReleaseWriterLock();
                }
            }
        }
        #endregion
    }
}
