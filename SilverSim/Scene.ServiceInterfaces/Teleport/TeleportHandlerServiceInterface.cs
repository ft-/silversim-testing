// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.ComponentModel;

namespace SilverSim.ServiceInterfaces.Teleport
{
    public abstract class TeleportHandlerServiceInterface : IAgentTeleportServiceInterface
    {
        protected TeleportHandlerServiceInterface()
        {

        }

        public abstract void Cancel();
        /* <summary>this is the local call for active teleport</summary> */
        [Description("Local call from remote call handlers")]
        public abstract void ReleaseAgent(UUID fromSceneID);
        public abstract void CloseAgentOnRelease(UUID fromSceneID);
        public abstract void EnableSimulator(UUID fromSceneID, IAgent agent, DestinationInfo destinationRegion);
        public abstract void DisableSimulator(UUID fromSceneID, IAgent agent, RegionInfo regionInfo);
        /* <summary>this is the remote call</summary> */
        [Description("Remote call to other simulators")]
        public abstract void ReleaseAgent(UUID fromSceneID, IAgent agent, RegionInfo regionInfo);
        public abstract GridType GridType { get; }

        /* following three functions return true if they accept a teleport request */
        public virtual bool TeleportTo(SceneInterface sceneInterface, IAgent agent, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            return false;
        }

        public virtual bool TeleportTo(SceneInterface sceneInterface, IAgent agent, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            return false;
        }

        public virtual bool TeleportTo(SceneInterface sceneInterface, IAgent agent, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            return false;
        }

    }
}
