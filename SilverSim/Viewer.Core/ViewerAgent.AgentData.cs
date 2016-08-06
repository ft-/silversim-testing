// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        void SendAgentDataUpdate(AgentCircuit circuit)
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

        [PacketHandler(MessageType.AgentDataUpdateRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAgentDataUpdateRequest(Message m)
        {
            Messages.Agent.AgentDataUpdateRequest adur = (Messages.Agent.AgentDataUpdateRequest)m;
            if (adur.AgentID == ID && adur.SessionID == adur.CircuitSessionID)
            {
                AgentCircuit circuit;
                if (Circuits.TryGetValue(adur.ReceivedOnCircuitCode, out circuit))
                {
                    SendAgentDataUpdate(circuit);
                }
            }
        }
    }
}
