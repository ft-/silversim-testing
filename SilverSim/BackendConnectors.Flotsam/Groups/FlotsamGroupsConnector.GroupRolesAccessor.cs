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

namespace SilverSim.BackendConnectors.Flotsam.Groups
{
    public partial class FlotsamGroupsConnector
    {
        class GroupRolesAccessor : FlotsamGroupsCommonConnector, IGroupRolesInterface
        {
            public GroupRolesAccessor(string uri)
                : base(uri)
            {
            }

            public GroupRole this[UUI requestingAgent, UGI group, UUID roleID]
            {
                get 
                {
                    foreach(GroupRole role in this[requestingAgent, group])
                    {
                        if(role.ID.Equals(roleID))
                        {
                            return role;
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            public List<GroupRole> this[UUI requestingAgent, UGI group]
            {
                get 
                {
                    List<GroupRole> roles = new List<GroupRole>();
                    Map m = new Map();
                    m.Add("GroupID", group.ID);
                    IValue iv = FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroupRoles", m);
                    if(!(iv is AnArray))
                    {
                        throw new AccessFailedException();
                    }
                    foreach(IValue v in (AnArray)iv)
                    {
                        if(v is Map)
                        {
                            roles.Add(v.ToGroupRole(group));
                        }
                    }
                    return roles;
                }
            }

            public List<GroupRole> this[UUI requestingAgent, UGI group, UUI principal]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("GroupID", group.ID);
                    m.Add("AgentID", principal.ID); 
                    IValue iv = FlotsamXmlRpcGetCall(requestingAgent, "groups.getAgentRoles", m);
                    List<GroupRole> rolemems = new List<GroupRole>();
                    if (iv is AnArray)
                    {
                        foreach (IValue v in ((AnArray)iv))
                        {
                            if (v is Map)
                            {
                                rolemems.Add(v.ToGroupRole(group));
                            }
                        }
                    }
                    return rolemems;
                }
            }

            public void Add(UUI requestingAgent, GroupRole role)
            {
                Map m = new Map();
                m.Add("GroupID", role.Group.ID);
                m.Add("RoleID", role.ID);
                m.Add("Name", role.Name);
                m.Add("Description", role.Description);
                m.Add("Title", role.Title);
                m.Add("Powers", ((ulong)role.Powers).ToString());
                FlotsamXmlRpcCall(requestingAgent, "groups.addRoleToGroup", m);
            }

            public void Update(UUI requestingAgent, GroupRole role)
            {
                Map m = new Map();
                m.Add("GroupID", role.Group.ID);
                m.Add("RoleID", role.ID);
                m.Add("Name", role.Name);
                m.Add("Description", role.Description);
                m.Add("Title", role.Title);
                m.Add("Powers", ((ulong)role.Powers).ToString());
                FlotsamXmlRpcCall(requestingAgent, "groups.updateGroupRole", m);
            }

            public void Delete(UUI requestingAgent, UGI group, UUID roleID)
            {
                Map m = new Map();
                m.Add("GroupID", group.ID);
                m.Add("RoleID", roleID);
                FlotsamXmlRpcCall(requestingAgent, "groups.removeRoleFromGroup", m);
            }
        }
    }
}
