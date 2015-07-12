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

using SilverSim.BackendConnectors.Robust.Common;
using SilverSim.Main.Common.HttpClient;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.BackendConnectors.Robust.GroupsV2
{
    public partial class RobustGroupsConnector
    {
        class RoleMembersAccessor : IGroupRolemembersInterface
        {
            public int TimeoutMs = 20000;
            string m_Uri;

            public RoleMembersAccessor(string uri)
            {
                m_Uri = uri;
            }

            public GroupRolemember this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get
                {
                    foreach(GroupRolemember member in this[requestingAgent, group])
                    {
                        if(member.RoleID.Equals(roleID) &&
                            member.Principal.EqualsGrid(principal))
                        {
                            return member;
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            public List<GroupRolemember> this[UUI requestingAgent, UGI group, UUID roleID]
            {
                get
                {
                    return new List<GroupRolemember>(this[requestingAgent, group].Where((member) => member.RoleID.Equals(roleID)));
                }
            }

            public List<GroupRolemember> this[UUI requestingAgent, UGI group]
            {
                get
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["GroupID"] = (string)group.ID;
                    post["RequestingAgentID"] = requestingAgent.ToString();
                    post["METHOD"] = "GETROLEMEMBERS";
                    Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                    if (!m.ContainsKey("RESULT"))
                    {
                        throw new KeyNotFoundException();
                    }
                    if (m["RESULT"].ToString() == "NULL")
                    {
                        throw new KeyNotFoundException();
                    }

                    List<GroupRolemember> rolemembers = new List<GroupRolemember>();
                    foreach (IValue iv in ((Map)m["RESULT"]).Values)
                    {
                        if (iv is Map)
                        {
                            GroupRolemember member = iv.ToGroupRolemember();
                            member.Group = group;
                            rolemembers.Add(member);
                        }
                    }

                    return rolemembers;
                }
            }


            public void Add(UUI requestingAgent, GroupRolemember rolemember)
            {
                Dictionary<string, string> post = rolemember.ToPost();
                post["GroupID"] = (string)rolemember.Group.ID;
                post["RequestingAgentID"] = requestingAgent.ToString();
                post["OP"] = "ADD";
                post["METHOD"] = "AGENTROLE";
                BooleanResponseRequest(m_Uri, post, false, TimeoutMs);
            }

            public void Delete(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
            {
                Dictionary<string, string> post = new Dictionary<string,string>();
                post["GroupID"] = (string)group.ID;
                post["RoleID"] = (string)roleID;
                post["AgentID"] = principal.ToString();
                post["RequestingAgentID"] = requestingAgent.ToString();
                post["OP"] = "DELETE";
                post["METHOD"] = "AGENTROLE";
                BooleanResponseRequest(m_Uri, post, false, TimeoutMs);
            }
        }
    }
}
