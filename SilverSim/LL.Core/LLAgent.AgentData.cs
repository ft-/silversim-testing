/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.LL.Messages;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        [PacketHandler(MessageType.AgentDataUpdateRequest)]
        void HandleAgentDataUpdateRequest(Message m)
        {
            Messages.Agent.AgentDataUpdateRequest adur = (Messages.Agent.AgentDataUpdateRequest)m;
            if (adur.AgentID == ID && adur.SessionID == adur.CircuitSessionID)
            {
                Circuit circuit;
                if (Circuits.TryGetValue(adur.ReceivedOnCircuitCode, out circuit))
                {
                    Messages.Agent.AgentDataUpdate adu = new Messages.Agent.AgentDataUpdate();
                    GroupsServiceInterface groupsService = circuit.Scene.GroupsService;
                    if (null != groupsService)
                    {
                        try
                        {
                            GroupRole gr;
                            GroupActiveMembership gm = groupsService.ActiveMembership[Owner, Owner];
                            adu.ActiveGroupID = groupsService.ActiveGroup[Owner, Owner].ID;
                            if (adu.ActiveGroupID != UUID.Zero)
                            {
                                gr = groupsService.Roles[Owner, gm.Group, gm.SelectedRoleID];
                                adu.GroupName = gm.Group.GroupName;
                                adu.GroupTitle = gr.Title;
                                adu.GroupPowers = gr.Powers;
                            }
                        }
                        catch
#if DEBUG
                            (Exception e)
#endif
                        {
#if DEBUG
                            m_Log.Debug("HandleAgentDataUpdateRequest", e);
#endif
                            adu.ActiveGroupID = UUID.Zero;
                            adu.GroupName = string.Empty;
                            adu.GroupTitle = string.Empty;
                            adu.GroupPowers = GroupPowers.None;
                        }
                    }
                    adu.AgentID = ID;
                    adu.FirstName = FirstName;
                    adu.LastName = LastName;
                    circuit.SendMessage(adu);
                }
            }
        }
    }
}
