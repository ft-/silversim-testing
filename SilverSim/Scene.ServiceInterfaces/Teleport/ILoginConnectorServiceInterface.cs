// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.Grid;

namespace SilverSim.Scene.ServiceInterfaces.Teleport
{
    public interface ILoginConnectorServiceInterface
    {
        void LoginTo(SessionInfo sessionInfo, DestinationInfo destinationInfo, CircuitInfo circuitInfo, AppearanceInfo appearance, TeleportFlags flags);
    }
}
