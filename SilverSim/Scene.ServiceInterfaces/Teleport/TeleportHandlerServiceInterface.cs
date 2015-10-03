// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Grid;

namespace SilverSim.ServiceInterfaces.Teleport
{
    public abstract class TeleportHandlerServiceInterface : IAgentTeleportServiceInterface
    {
        public TeleportHandlerServiceInterface()
        {

        }

        public abstract void Cancel();
        public abstract void ReleaseAgent(UUID fromSceneID);
        public abstract void CloseAgentOnRelease(UUID fromSceneID);
        public abstract void EnableSimulator(UUID fromSceneID, IAgent agent, DestinationInfo destinationRegion);
    }
}
