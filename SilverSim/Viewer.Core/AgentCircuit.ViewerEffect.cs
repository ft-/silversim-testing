// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Appearance;
using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Scene.Types.Scene;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.ViewerEffect)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleViewerEffect(Message m)
        {
            var ve = (ViewerEffect)m;
            if(ve.AgentID != ve.CircuitAgentID ||
                ve.SessionID != ve.CircuitSessionID)
            {
                return;
            }

            /* we only route valid messages here but keep SessionID from being broadcasted */
            ve.SessionID = UUID.Zero;

            SceneInterface scene = Scene;
            ViewerAgent thisAgent = Agent;
            if(thisAgent == null || scene == null)
            {
                return;
            }
            UUI agentOwner = thisAgent.Owner;
            foreach (var agent in scene.Agents)
            {
                if (agent.Owner.Equals(agentOwner))
                {
                    continue;
                }
                agent.SendMessageAlways(m, scene.ID);
            }
        }
    }
}
