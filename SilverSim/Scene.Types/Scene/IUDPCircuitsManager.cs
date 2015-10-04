// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Types;
using SilverSim.Scene.Types.Object;

namespace SilverSim.Scene.Types.Scene
{
    public interface IUDPCircuitsManager
    {
        void SendMessageToAgent(UUID agentID, Message m);
        void ScheduleUpdate(ObjectUpdateInfo info);
    }
}
