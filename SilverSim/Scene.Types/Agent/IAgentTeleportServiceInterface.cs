// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;

namespace SilverSim.Scene.Types.Agent
{
    /* this interface is needed so we can resolve a cyclic reference */
    public interface IAgentTeleportServiceInterface
    {
        void Cancel();
        void ReleaseAgent(UUID fromSceneID);
        void CloseAgentOnRelease(UUID fromSceneID);
        void EnableSimulator(IAgent agent, DestinationInfo destinationRegion);
    }
}
