﻿/*

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
using SilverSim.Types;
using SilverSim.Types.Groups;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        public void HandleAgentDataUpdateRequest(Message m)
        {
            Messages.Agent.AgentDataUpdateRequest adur = (Messages.Agent.AgentDataUpdateRequest)m;
            if (adur.AgentID == ID && adur.SessionID == adur.CircuitSessionID)
            {
                Circuit circuit;
                if (Circuits.TryGetValue(adur.ReceivedOnCircuitCode, out circuit))
                {
                    Messages.Agent.AgentDataUpdate adu = new Messages.Agent.AgentDataUpdate();
                    if(null != circuit.Agent.GroupsService)
                    {
                        try
                        {
                            GroupInfo gi;
                            GroupMember gm;
                            GroupRole gr;
                            adu.ActiveGroupID = circuit.Agent.GroupsService.ActiveGroup[Owner, Owner];
                            if (adu.ActiveGroupID != UUID.Zero)
                            {
                                gi = circuit.Agent.GroupsService.Groups[Owner, adu.ActiveGroupID];
                                gm = circuit.Agent.GroupsService.Members[Owner, gi.ID, Owner];
                                gr = circuit.Agent.GroupsService.Roles[Owner, gi.ID, gm.SelectedRoleID];
                                adu.GroupName = gi.Name;
                                adu.GroupTitle = gr.Title;
                                adu.GroupPowers = gr.Powers;
                            }
                        }
                        catch
                        {
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
