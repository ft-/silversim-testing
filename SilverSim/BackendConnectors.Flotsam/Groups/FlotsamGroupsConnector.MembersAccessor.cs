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
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.BackendConnectors.Flotsam.Groups
{
    public partial class FlotsamGroupsConnector
    {
        class MembersAccessor : FlotsamGroupsCommonConnector, IGroupMembersInterface
        {
            public MembersAccessor(string uri)
                : base(uri)
            {
            }

            public GroupMember this[UUI requestingAgent, UGI group, UUI principal]
            {
                get 
                {
                    IEnumerable<GroupMember> e = this[requestingAgent, group].Where(p => p.Principal.ID == principal.ID);
                    foreach(GroupMember g in e)
                    {
                        return g;
                    }
                    throw new KeyNotFoundException();
                }
            }

            public List<GroupMember> this[UUI requestingAgent, UGI group]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("GroupID", group.ID);
                    IValue v = FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroupMembers", m);
                    if(!(v is AnArray))
                    {
                        throw new AccessFailedException();
                    }
                    List<GroupMember> gmems = new List<GroupMember>();
                    foreach(IValue iv in (AnArray)v)
                    {
                        if(iv is Map)
                        {
                            gmems.Add(iv.ToGroupMember(group));
                        }
                    }
                    return gmems;
                }
            }

            public List<GroupMember> this[UUI requestingAgent, UUI principal]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("AgentID", principal.ID);
                    IValue v = FlotsamXmlRpcGetCall(requestingAgent, "groups.getAgentGroupMemberships", m);
                    if (!(v is AnArray))
                    {
                        throw new AccessFailedException();
                    }
                    List<GroupMember> gmems = new List<GroupMember>();
                    foreach (IValue iv in (AnArray)v)
                    {
                        if (iv is Map)
                        {
                            gmems.Add(iv.ToGroupMember());
                        }
                    }
                    return gmems;
                }
            }

            public GroupMember Add(UUI requestingAgent, UGI group, UUI principal, UUID roleID, string accessToken)
            {
                Map m = new Map();
                m.Add("AgentID", principal.ID);
                m.Add("GroupID", group.ID);
                FlotsamXmlRpcCall(requestingAgent, "groups.addAgentToGroup", m);
                return this[requestingAgent, group, principal];
            }

            public void Delete(UUI requestingAgent, UGI group, UUI principal)
            {
                Map m = new Map();
                m.Add("AgentID", principal.ID);
                m.Add("GroupID", group.ID);
                FlotsamXmlRpcCall(requestingAgent, "groups.removeAgentFromGroup", m);
            }
        }
    }
}
