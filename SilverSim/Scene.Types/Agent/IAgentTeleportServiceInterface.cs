// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
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

        /* following five functions return true if they accept a teleport request or if they want to distribute more specific error messages except region not found */
        bool TeleportTo(SceneInterface sceneInterface, IAgent agent, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, IAgent agent, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, IAgent agent, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, IAgent agent, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, IAgent agent, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags);
    }
}
