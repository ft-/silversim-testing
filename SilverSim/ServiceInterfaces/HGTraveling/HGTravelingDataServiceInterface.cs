// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.HGTraveling;

namespace SilverSim.ServiceInterfaces.HGTraveling
{
    public abstract class HGTravelingDataServiceInterface
    {
        protected HGTravelingDataServiceInterface()
        {

        }

        public abstract HGTravelingData GetHGTravelingData(UUID sessionID);
        public abstract HGTravelingData GetHGTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress);
        public abstract HGTravelingData GetHGTravelingDatasByAgentUUID(UUID agentID);
        public abstract HGTravelingData GetHGTravelignDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI);
        public abstract void Store(HGTravelingData data);
        public abstract bool Remove(UUID sessionID);
        public abstract bool RemoveByAgentUUID(UUID agentID);
    }
}
