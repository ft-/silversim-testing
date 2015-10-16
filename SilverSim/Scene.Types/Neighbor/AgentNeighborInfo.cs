// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types.Grid;

namespace SilverSim.Scene.Types.Neighbor
{
    public struct AgentChildInfo
    {
        public IAgentTeleportServiceInterface TeleportService;
        public IAgentChildUpdateServiceInterface ChildAgentUpdateService;
        public DestinationInfo DestinationInfo;
    }
}
