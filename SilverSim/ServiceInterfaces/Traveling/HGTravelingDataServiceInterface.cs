// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.TravelingData;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Traveling
{
    public abstract class TravelingDataServiceInterface
    {
        protected TravelingDataServiceInterface()
        {

        }

        public abstract TravelingDataInfo GetTravelingData(UUID sessionID);
        public abstract TravelingDataInfo GetTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress);
        public abstract List<TravelingDataInfo> GetTravelingDatasByAgentUUID(UUID agentID);
        public abstract TravelingDataInfo GetTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI);
        public abstract void Store(TravelingDataInfo data);
        public abstract bool Remove(UUID sessionID);
        public abstract bool RemoveByAgentUUID(UUID agentID);
    }
}
