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
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Viewer.Messages.Agent;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private void SendAgentDataUpdate(AgentCircuit circuit)
        {
            var adu = new AgentDataUpdate();
            var groupsService = circuit.Scene.GroupsService;
            if (groupsService != null)
            {
                try
                {
                    GroupRole gr;
                    GroupActiveMembership gm;
                    if (groupsService.ActiveMembership.TryGetValue(Owner, Owner, out gm))
                    {
                        adu.ActiveGroupID = groupsService.ActiveGroup[Owner, Owner].ID;
                        if (adu.ActiveGroupID != UUID.Zero)
                        {
                            gr = groupsService.Roles[Owner, gm.Group, gm.SelectedRoleID];
                            adu.GroupName = gm.Group.GroupName;
                            adu.GroupTitle = gr.Title;
                            adu.GroupPowers = gr.Powers;
                        }
                    }
                }
                catch
#if DEBUG
                            (Exception e)
#endif
                {
                    /* only needed for debugging purposes to show. Otherwise, it gets pretty spammy during normal operation */
#if DEBUG
                    m_Log.Debug("HandleAgentDataUpdateRequest", e);
#endif
                }
            }
            adu.AgentID = ID;
            adu.FirstName = FirstName;
            adu.LastName = LastName;
            circuit.SendMessage(adu);
        }

        [PacketHandler(MessageType.AgentDataUpdateRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleAgentDataUpdateRequest(Message m)
        {
            var adur = (AgentDataUpdateRequest)m;
            if (adur.AgentID == ID && adur.SessionID == adur.CircuitSessionID)
            {
                AgentCircuit circuit;
                if (Circuits.TryGetValue(adur.CircuitSceneID, out circuit))
                {
                    SendAgentDataUpdate(circuit);
                }
            }
        }
    }
}
