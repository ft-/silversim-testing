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
        class NoticesAccessor : IGroupNoticesInterface
        {
            public int TimeoutMs = 20000;
            string m_Uri;

            public NoticesAccessor(string uri)
            {
                m_Uri = uri;
            }

            public List<GroupNotice> GetNotices(UUI requestingAgent, UGI group)
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["GroupID"] = (string)group.ID;
                post["RequestingAgentID"] = requestingAgent.ToString();
                post["METHOD"] = "GETNOTICES";
                Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                if (!m.ContainsKey("RESULT"))
                {
                    throw new KeyNotFoundException();
                }
                if (m["RESULT"].ToString() == "NULL")
                {
                    throw new KeyNotFoundException();
                }

                List<GroupNotice> groupnotices = new List<GroupNotice>();
                foreach (IValue iv in ((Map)m["RESULT"]).Values)
                {
                    if (iv is Map)
                    {
                        GroupNotice notice = new GroupNotice();
                        notice.Group = group;
                        groupnotices.Add(notice);
                    }
                }

                return groupnotices;
            }

            public GroupNotice this[UUI requestingAgent, UUID groupNoticeID]
            {
                get 
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["InviteID"] = (string)groupNoticeID;
                    post["RequestingAgentID"] = requestingAgent.ToString();
                    post["OP"] = "GET";
                    post["METHOD"] = "INVITE";
                    Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                    if (!m.ContainsKey("RESULT"))
                    {
                        throw new AccessFailedException();
                    }
                    if (m["RESULT"].ToString() == "NULL")
                    {
                        throw new AccessFailedException(m["REASON"].ToString());
                    }

                    GroupNotice notice = m["RESULT"].ToGroupNotice();
#warning TODO: GroupNotice service does not deliver any group ID in response
                    return notice;
                }
            }

            public void Add(UUI requestingAgent, GroupNotice notice)
            {
                Dictionary<string, string> post = notice.ToPost();
                post["GroupID"] = (string)notice.Group.ID;
                post["RequestingAgentID"] = requestingAgent.ToString();
                post["METHOD"] = "ADDNOTICE";
                BooleanResponseRequest(m_Uri, post, false, TimeoutMs);
            }

            public void Delete(UUI requestingAgent, UUID groupNoticeID)
            {
                throw new NotImplementedException();
            }
        }
    }
}
