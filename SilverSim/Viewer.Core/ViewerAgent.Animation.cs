// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Avatar;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        protected override void SendAnimations(AvatarAnimation m)
        {
            Circuits.ForEach(delegate(AgentCircuit c)
            {
                c.Scene.SendAgentAnimToAllAgents(m);
            });
        }

        [PacketHandler(MessageType.AgentAnimation)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAgentAnimation(Message m)
        {
            AgentAnimation req = (AgentAnimation)m;

            foreach(AgentAnimation.AnimationEntry e in req.AnimationEntryList)
            {
                if(e.StartAnim)
                {
                    PlayAnimation(e.AnimID, UUID.Zero);
                }
                else
                {
                    StopAnimation(e.AnimID, UUID.Zero);
                }
            }
        }
    }
}
