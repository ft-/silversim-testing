// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Traveling;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.TravelingData;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.TravelingData
{
    #region Service implementation
    public class MemoryTravelingDataService : TravelingDataServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly RwLockedDictionary<UUID, TravelingDataInfo> m_HGTravelingDatas = new RwLockedDictionary<UUID, TravelingDataInfo>();

        public MemoryTravelingDataService()
        {

        }

        public override TravelingDataInfo GetTravelingData(UUID sessionID)
        {
            return m_HGTravelingDatas[sessionID];
        }

        public override TravelingDataInfo GetTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress)
        {
            foreach(TravelingDataInfo hgd in m_HGTravelingDatas.Values)
            {
                if(hgd.ClientIPAddress == ipAddress && hgd.UserID == agentID)
                {
                    return hgd;
                }
            }
            throw new KeyNotFoundException();
        }

        public override TravelingDataInfo GetTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI)
        {
            foreach (TravelingDataInfo hgd in m_HGTravelingDatas.Values)
            {
                if (hgd.GridExternalName != homeURI && hgd.UserID == agentID)
                {
                    return hgd;
                }
            }
            throw new KeyNotFoundException();
        }

        public override List<TravelingDataInfo> GetTravelingDatasByAgentUUID(UUID agentID)
        {
            List<TravelingDataInfo> hgds = new List<TravelingDataInfo>();
            foreach(TravelingDataInfo hgd in m_HGTravelingDatas.Values)
            {
                if(hgd.UserID == agentID)
                {
                    hgds.Add(hgd);
                }
            }
            return hgds;
        }

        public override bool Remove(UUID sessionID)
        {
            return m_HGTravelingDatas.Remove(sessionID);
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            RemoveByAgentUUID(accountID);
        }

        public override bool RemoveByAgentUUID(UUID agentID)
        {
            List<UUID> sessionIds = new List<UUID>();
            foreach(KeyValuePair<UUID, TravelingDataInfo> kvp in m_HGTravelingDatas)
            {
                if(kvp.Value.UserID == agentID)
                {
                    sessionIds.Add(kvp.Key);
                }
            }

            bool f = false;
            foreach(UUID id in sessionIds)
            {
                if(m_HGTravelingDatas.Remove(id))
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
            m_HGTravelingDatas[data.SessionID] = data;
        }
    }
    #endregion

    #region Factory
    [PluginName("TravelingData")]
    public class MemoryTravelingDataServiceFactory : IPluginFactory
    {
        public MemoryTravelingDataServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryTravelingDataService();
        }
    }
    #endregion
}
