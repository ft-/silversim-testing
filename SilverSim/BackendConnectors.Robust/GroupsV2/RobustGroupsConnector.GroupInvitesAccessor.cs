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
        class InvitesAccessor : IGroupInvitesInterface
        {
            public int TimeoutMs = 20000;
            string m_Uri;

            public InvitesAccessor(string uri)
            {
                m_Uri = uri;
            }

            public GroupInvite this[UUI requestingAgent, UUID groupInviteID]
            {
                get 
                { 
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["InviteID"] = (string)groupInviteID;
                    post["RequestingAgentID"] = (string)requestingAgent.ID;
                    post["OP"] = "GET";
                    post["METHOD"] = "INVITE";

                    Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                    if (!m.ContainsKey("RESULT"))
                    {
                        throw new KeyNotFoundException();
                    }
                    if (m["RESULT"].ToString() == "NULL")
                    {
                        throw new KeyNotFoundException();
                    }

                    Map resultMap = m["RESULT"] as Map;
                    GroupInvite gi = new GroupInvite();
                    gi.ID = resultMap["InviteID"].AsUUID;
                    gi.Group.ID = resultMap["GroupID"].AsUUID;
                    gi.RoleID = resultMap["RoleID"].AsUUID;
                    gi.Principal.ID = resultMap["AgentID"].AsUUID;

                    return gi;
                }
            }

            public List<GroupInvite> this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get 
                {
                    return new List<GroupInvite>();
                }
            }

            public List<GroupInvite> this[UUI requestingAgent, UUI principal]
            {
                get
                {
                    return new List<GroupInvite>();
                }
            }

            public List<GroupInvite> GetByGroup(UUI requestingAgent, UGI group)
            {
                return new List<GroupInvite>();
            }

            public void Add(UUI requestingAgent, GroupInvite invite)
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["InviteID"] = (string)invite.ID;
                post["GroupID"] = (string)invite.Group.ID;
                post["RoleID"] = (string)invite.RoleID;
                post["AgentID"] = (string)invite.Principal.ID;
                post["RequestingAgentID"] = (string)requestingAgent.ID;
                post["OP"] = "ADD";
                post["METHOD"] = "INVITE";
                BooleanResponseRequest(m_Uri, post, false, TimeoutMs);
            }

            public void Delete(UUI requestingAgent, UUID inviteID)
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["METHOD"] = "INVITE";
                post["OP"] = "DELETE";
                post["RequestingAgentID"] = (string)requestingAgent.ID;
                post["InviteID"] = (string)inviteID;
                BooleanResponseRequest(m_Uri, post, false, TimeoutMs);
            }
        }
    }
}
