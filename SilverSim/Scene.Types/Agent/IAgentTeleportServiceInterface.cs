// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.ComponentModel;

namespace SilverSim.Scene.Types.Agent
{
    /* this interface is needed so we can resolve a cyclic reference */
    public interface IAgentTeleportServiceInterface
    {
        void Cancel();
        /* <summary>this is the local call for active teleport</summary> */
        [Description("Local call from remote call handlers")]
        void ReleaseAgent(UUID fromSceneID);
        void CloseAgentOnRelease(UUID fromSceneID);
        void DisableSimulator(UUID fromSceneID, IAgent agent, RegionInfo regionInfo);
        void EnableSimulator(UUID fromSceneID, IAgent agent, DestinationInfo destinationRegion);
        /* <summary>this is the remote call</summary> */
        [Description("Remote call to other simulators")]
        void ReleaseAgent(UUID fromSceneID, IAgent agent, RegionInfo regionInfo);
        GridType GridType { get; }
    }
}
