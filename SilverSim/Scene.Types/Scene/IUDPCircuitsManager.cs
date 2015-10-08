// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Types;
using SilverSim.Scene.Types.Object;
using System.Net;

namespace SilverSim.Scene.Types.Scene
{
    public interface IUDPCircuitsManager
    {
        void SendMessageToAgent(UUID agentID, Message m);
        void ScheduleUpdate(ObjectUpdateInfo info);
        ICircuit UseSimCircuit(IPEndPoint ep, UUID sessionID, SceneInterface thisScene, UUID remoteSceneID, uint circuitcode, GridVector remoteLocation, Vector3 remoteOffset);
    }
}
