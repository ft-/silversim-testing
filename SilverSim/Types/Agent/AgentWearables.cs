// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            for(WearableType type = WearableType.Shape; type < WearableType.NumWearables; ++type)
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
                    List<WearableInfo> l = new List<WearableInfo>();
                    foreach(WearableInfo i in s)
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
                List<WearableInfo> nl = new List<WearableInfo>();
                if(value.Count > 5)
                {
                    throw new ArgumentException("Too many elements in list");
                }
                /* do not give access to our internal data */
                foreach(WearableInfo wi in value)
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
                    List<WearableInfo> wearableList = m_Wearables[type];
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
        public static implicit operator Dictionary<WearableType, List<WearableInfo>>(AgentWearables aw)
        {
            return aw.All;
        }

        public Dictionary<WearableType, List<WearableInfo>> All
        {
            get
            {
                m_WearablesUpdateLock.AcquireReaderLock(-1);
                try
                {
                    Dictionary<WearableType, List<WearableInfo>> od = new Dictionary<WearableType, List<WearableInfo>>();
                    foreach(KeyValuePair<WearableType, List<WearableInfo>> kvp in m_Wearables)
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
                    foreach(List<WearableInfo> lwi in m_Wearables.Values)
                    {
                        lwi.Clear();
                    }

                    foreach (KeyValuePair<WearableType, List<WearableInfo>> kvp in value)
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
