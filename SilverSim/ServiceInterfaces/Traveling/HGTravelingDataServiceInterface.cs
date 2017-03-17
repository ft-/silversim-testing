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
