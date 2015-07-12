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

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Flotsam.Groups
{
    public partial class FlotsamGroupsConnector
    {
        class InvitesAccessor : FlotsamGroupsCommonConnector, IGroupInvitesInterface
        {
            public InvitesAccessor(string uri)
                : base(uri)
            {
            }

            public GroupInvite this[UUI requestingAgent, UUID groupInviteID]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("InviteID", groupInviteID);
                    IValue iv = FlotsamXmlRpcGetCall(requestingAgent, "groups.getAgentToGroupInvite", m);
                    if(!(iv is Map))
                    {
                        throw new AccessFailedException();
                    }
                    m = (Map)iv;

                    GroupInvite inv = new GroupInvite();
                    inv.ID = groupInviteID;
                    inv.Principal.ID = m["AgentID"].AsUUID;
                    inv.Group.ID = m["GroupID"].AsUUID;
                    inv.RoleID = m["RoleID"].AsUUID;
                    return inv;
                }
            }

            public List<GroupInvite> this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get { throw new NotImplementedException(); }
            }

            public List<GroupInvite> this[UUI requestingAgent, UUI principal]
            {
                get { throw new NotImplementedException(); }
            }

            public List<GroupInvite> GetByGroup(UUI requestingAgent, UGI group)
            {
                throw new NotImplementedException();
            }

            public void Add(UUI requestingAgent, GroupInvite invite)
            {
                Map m = new Map();
                m.Add("InviteID", invite.ID);
                m.Add("GroupID", invite.Group.ID);
                m.Add("RoleID", invite.RoleID);
                m.Add("AgentID", invite.Principal.ID);
                FlotsamXmlRpcCall(requestingAgent, "groups.addAgentToGroupInvite", m);
            }

            public void Delete(UUI requestingAgent, UUID inviteID)
            {
                Map m = new Map();
                m.Add("InviteID", inviteID);
                FlotsamXmlRpcCall(requestingAgent, "groups.removeAgentToGroupInvite", m);
            }
        }
    }
}
