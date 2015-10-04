// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Appearance;
using SilverSim.Scene.Types.Agent;
using SilverSim.Types;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.ViewerEffect)]
        void HandleViewerEffect(Message m)
        {
            ViewerEffect ve = (ViewerEffect)m;
            if(ve.AgentID != ve.CircuitAgentID ||
                ve.SessionID != ve.CircuitSessionID)
            {
                return;
            }

            /* we only route valid messages here but keep SessionID from being broadcasted */
            ve.SessionID = UUID.Zero;
            foreach(IAgent agent in Scene.Agents)
            {
                if(agent.Owner.Equals(Agent.Owner))
                {
                    continue;
                }
                agent.SendMessageAlways(m, Scene.ID);
            }
        }
    }
}
