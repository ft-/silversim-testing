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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Traveling;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.TravelingData;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.TravelingData
{
    [Description("Memory TravelingData backend")]
    [PluginName("TravelingData")]
    public class MemoryTravelingDataService : TravelingDataServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private readonly RwLockedDictionary<UUID, TravelingDataInfo> m_HGTravelingDatas = new RwLockedDictionary<UUID, TravelingDataInfo>();

        public override TravelingDataInfo GetTravelingData(UUID sessionID) =>
            m_HGTravelingDatas[sessionID];

        public override TravelingDataInfo GetTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress)
        {
            foreach(var hgd in m_HGTravelingDatas.Values)
            {
                if(hgd.ClientIPAddress == ipAddress && hgd.UserID == agentID)
                {
                    return new TravelingDataInfo(hgd);
                }
            }
            throw new KeyNotFoundException();
        }

        public override TravelingDataInfo GetTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI)
        {
            foreach (var hgd in m_HGTravelingDatas.Values)
            {
                if (hgd.GridExternalName != homeURI && hgd.UserID == agentID)
                {
                    return new TravelingDataInfo(hgd);
                }
            }
            throw new KeyNotFoundException();
        }

        public override List<TravelingDataInfo> GetTravelingDatasByAgentUUID(UUID agentID)
        {
            var hgds = new List<TravelingDataInfo>();
            foreach(var hgd in m_HGTravelingDatas.Values)
            {
                if(hgd.UserID == agentID)
                {
                    hgds.Add(new TravelingDataInfo(hgd));
                }
            }
            return hgds;
        }

        public override bool Remove(UUID sessionID, out TravelingDataInfo info) =>
            m_HGTravelingDatas.Remove(sessionID, out info);

        public void Remove(UUID scopeID, UUID accountID)
        {
            RemoveByAgentUUID(accountID);
        }

        public override bool RemoveByAgentUUID(UUID agentID, out TravelingDataInfo info)
        {
            info = default(TravelingDataInfo);
            var sessionIds = new List<UUID>();
            foreach(var kvp in m_HGTravelingDatas)
            {
                if(kvp.Value.UserID == agentID)
                {
                    sessionIds.Add(kvp.Key);
                }
            }

            bool f = false;
            foreach(var id in sessionIds)
            {
                if(m_HGTravelingDatas.Remove(id, out info))
                {
                    f = true;
                }
            }
            return f;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override void Store(TravelingDataInfo data)
        {
            m_HGTravelingDatas[data.SessionID] = new TravelingDataInfo(data);
        }
    }
}
