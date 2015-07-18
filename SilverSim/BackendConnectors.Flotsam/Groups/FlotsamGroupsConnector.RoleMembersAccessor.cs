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
        class RoleMembersAccessor : FlotsamGroupsCommonConnector, IGroupRolemembersInterface
        {
            public RoleMembersAccessor(string uri)
                : base(uri)
            {
            }

            public GroupRolemember this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("GroupID", group.ID);
                    m.Add("AgentID", principal.ID);
                    IValue iv = FlotsamXmlRpcGetCall(requestingAgent, "groups.getAgentRoles", m);
                    if (iv is AnArray)
                    {
                        foreach (IValue v in ((AnArray)iv))
                        {
                            if (v is Map)
                            {
                                GroupRolemember gmem = v.ToGroupRolemember(group);
                                if(gmem.RoleID.Equals(roleID))
                                {
                                    return gmem;
                                }
                            }
                        }
                    }

                    throw new KeyNotFoundException();
                }
            }

            public List<GroupRolemembership> this[UUI requestingAgent, UUI principal]
            {
                get
                {
                    List<GroupRolemembership> gmems = new List<GroupRolemembership>();
                    Map m = new Map();
                    m.Add("AgentID", principal.ID);
                    IValue iv = FlotsamXmlRpcGetCall(requestingAgent, "groups.getAgentRoles", m);
                    if (iv is AnArray)
                    {
                        foreach (IValue v in ((AnArray)iv))
                        {
                            if (v is Map)
                            {
                                GroupRolemembership gmem = v.ToGroupRolemembership();
                                gmems.Add(gmem);
                            }
                        }
                    }
                    return gmems;
                }
            }

            public List<GroupRolemember> this[UUI requestingAgent, UGI group, UUID roleID]
            {
                get 
                {
                    return new List<GroupRolemember>(this[requestingAgent, group].Where(g => g.RoleID.Equals(roleID)));
                }
            }

            public List<GroupRolemember> this[UUI requestingAgent, UGI group]
            {
                get
                {
                    Map m = new Map();
                    m.Add("GroupID", group.ID);
                    IValue iv = FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroupRoleMembers", m);
                    List<GroupRolemember> rolemems = new List<GroupRolemember>();
                    if(iv is AnArray)
                    {
                        foreach(IValue v in ((AnArray)iv))
                        {
                            if(v is Map)
                            {
                                rolemems.Add(v.ToGroupRolemember(group));
                            }
                        }
                    }
                    return rolemems;
                }
            }

            public void Add(UUI requestingAgent, GroupRolemember rolemember)
            {
                Map m = new Map();
                m.Add("AgentID", rolemember.Principal.ID);
                m.Add("GroupID", rolemember.Group.ID);
                m.Add("RoleID", rolemember.RoleID);
                FlotsamXmlRpcCall(requestingAgent, "groups.addAgentToGroupRole", m);
            }

            public void Delete(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
            {
                Map m = new Map();
                m.Add("AgentID", principal.ID);
                m.Add("GroupID", group.ID);
                m.Add("RoleID", roleID);
                FlotsamXmlRpcCall(requestingAgent, "groups.removeAgentFromGroupRole", m);
            }
        }
    }
}
