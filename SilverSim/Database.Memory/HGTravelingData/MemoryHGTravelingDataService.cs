// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.HGTraveling;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.HGTraveling;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.HGTravelingData
{
    #region Service implementation
    public class MemoryHGTravelingDataService : HGTravelingDataServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly RwLockedDictionary<UUID, HGTravelingDataInfo> m_HGTravelingDatas = new RwLockedDictionary<UUID, HGTravelingDataInfo>();

        public MemoryHGTravelingDataService()
        {

        }

        public override HGTravelingDataInfo GetHGTravelingData(UUID sessionID)
        {
            return m_HGTravelingDatas[sessionID];
        }

        public override HGTravelingDataInfo GetHGTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress)
        {
            foreach(HGTravelingDataInfo hgd in m_HGTravelingDatas.Values)
            {
                if(hgd.ClientIPAddress == ipAddress && hgd.UserID == agentID)
                {
                    return hgd;
                }
            }
            throw new KeyNotFoundException();
        }

        public override HGTravelingDataInfo GetHGTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI)
        {
            foreach (HGTravelingDataInfo hgd in m_HGTravelingDatas.Values)
            {
                if (hgd.GridExternalName != homeURI && hgd.UserID == agentID)
                {
                    return hgd;
                }
            }
            throw new KeyNotFoundException();
        }

        public override List<HGTravelingDataInfo> GetHGTravelingDatasByAgentUUID(UUID agentID)
        {
            List<HGTravelingDataInfo> hgds = new List<HGTravelingDataInfo>();
            foreach(HGTravelingDataInfo hgd in m_HGTravelingDatas.Values)
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
            foreach(KeyValuePair<UUID, HGTravelingDataInfo> kvp in m_HGTravelingDatas)
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

        public override void Store(HGTravelingDataInfo data)
        {
            m_HGTravelingDatas[data.SessionID] = data;
        }
    }
    #endregion

    #region Factory
    [PluginName("HGTravelingData")]
    public class MemoryHGTravelingDataServiceFactory : IPluginFactory
    {
        public MemoryHGTravelingDataServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryHGTravelingDataService();
        }
    }
    #endregion
}
