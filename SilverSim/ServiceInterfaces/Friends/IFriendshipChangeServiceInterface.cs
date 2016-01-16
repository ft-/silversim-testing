// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Friends;

namespace SilverSim.ServiceInterfaces.Friends
{
    public interface IFriendshipChangeServiceInterface
    {
        void Offered(UUI fromAgent, UUI toAgent, string message);
        void Approved(UUI fromAgent, UUI toAgent);
        void Denied(UUI fromAgent, UUI toAgent);
        void Terminated(UUI fromAgent, UUI toAgent);
        void GrantRights(UUI fromAgent, UUI toAgent, FriendRightFlags oldRights, FriendRightFlags newRights);
    }
}
