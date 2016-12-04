// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.HGTraveling;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.HGTraveling
{
    public abstract class HGTravelingDataServiceInterface
    {
        protected HGTravelingDataServiceInterface()
        {

        }

        public abstract HGTravelingDataInfo GetHGTravelingData(UUID sessionID);
        public abstract HGTravelingDataInfo GetHGTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress);
        public abstract List<HGTravelingDataInfo> GetHGTravelingDatasByAgentUUID(UUID agentID);
        public abstract HGTravelingDataInfo GetHGTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI);
        public abstract void Store(HGTravelingDataInfo data);
        public abstract bool Remove(UUID sessionID);
        public abstract bool RemoveByAgentUUID(UUID agentID);
    }
}
