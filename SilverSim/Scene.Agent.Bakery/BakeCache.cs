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

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scene.Agent.Bakery
{
    public class BakeCache : IDisposable
    {
        private readonly object m_Lock = new object();
        public AssetServiceInterface AssetService { get; }

        public BakeCache(AssetServiceInterface assetService)
        {
            AssetService = assetService;
        }

        ~BakeCache()
        {
            Dispose();
        }

        private readonly Dictionary<UUID, AbstractSubBaker> m_SubBakers = new Dictionary<UUID, AbstractSubBaker>();
        private readonly Dictionary<UUID, OutfitItem> m_Items = new Dictionary<UUID, OutfitItem>();

        public List<AbstractSubBaker> SubBakers
        {
            get
            {
                lock (m_Lock)
                {
                    return new List<AbstractSubBaker>(m_SubBakers.Values);
                }
            }
        }

        public List<Wearable> Wearables
        {
            get
            {
                lock(m_Lock)
                {
                    return new List<Wearable>(from item in m_Items select item.Value.Wearable);
                }
            }
        }
        
        public void SetCurrentOutfit(Dictionary<UUID, OutfitItem> outfititems, AssetServiceInterface assetService)
        {
            lock (m_Lock)
            {
                List<UUID> removesubbakers = new List<UUID>(from subid in m_SubBakers.Keys where !outfititems.ContainsKey(subid) select subid);
                foreach (UUID itemid in removesubbakers)
                {
                    AbstractSubBaker sub;
                    if (m_SubBakers.TryGetValue(itemid, out sub))
                    {
                        m_SubBakers.Remove(itemid);
                        sub.Dispose();
                    }
                }

                foreach (KeyValuePair<UUID, OutfitItem> kvp in outfititems)
                {
                    AbstractSubBaker subbaker;
                    if (m_SubBakers.TryGetValue(kvp.Key, out subbaker))
                    {
                        subbaker.Ordinal = kvp.Value.Ordinal;
                    }
                    else
                    {
                        subbaker = kvp.Value.Wearable.CreateSubBaker();
                        if (subbaker != null)
                        {
                            subbaker.Ordinal = kvp.Value.Ordinal;
                            m_SubBakers.Add(kvp.Key, subbaker);
                        }
                    }
                }

                m_Items.Clear();
                foreach(KeyValuePair<UUID, OutfitItem> item in outfititems)
                {
                    m_Items.Add(item.Key, item.Value);
                }
            }
        }

        public bool IsBaked
        {
            get
            {
                bool baked = true;
                lock (m_Lock)
                {
                    foreach(AbstractSubBaker subbaker in m_SubBakers.Values)
                    {
                        baked = baked && subbaker.IsBaked;
                    }
                }
                return baked;
            }
        }

        public void Dispose()
        {
            List<AbstractSubBaker> values;
            lock (m_Lock)
            {
                values = new List<AbstractSubBaker>(m_SubBakers.Values);
                SubBakers.Clear();
            }
            foreach(AbstractSubBaker sub in values)
            {
                sub.Dispose();
            }
        }
    }
}
