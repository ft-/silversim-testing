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
        class GroupsAccessor : IGroupsInterface
        {
            public int TimeoutMs = 20000;
            string m_GroupServiceURI;
            string m_Uri;

            public GroupsAccessor(string uri, string serviceURI)
            {
                m_Uri = uri;
                m_GroupServiceURI = serviceURI;
            }

            GroupInfo CreateOrUpdate(UUI requestingAgent, GroupInfo group, string op)
            {
                Dictionary<string, string> post = group.ToPost();
                post.Remove("OwnerRoleID");
                post["OP"] = op;
                post["METHOD"] = "PUTGROUP";
                Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                if (!m.ContainsKey("RESULT"))
                {
                    throw new AccessFailedException();
                }
                if (m["RESULT"].ToString() == "NULL")
                {
                    throw new AccessFailedException(m["REASON"].ToString());
                }

                return m["RESULT"].ToGroup(m_GroupServiceURI);
            }

            public GroupInfo Create(UUI requestingAgent, GroupInfo group)
            {
                return CreateOrUpdate(requestingAgent, group, "ADD");
            }

            public GroupInfo Update(UUI requestingAgent, GroupInfo group)
            {
                return CreateOrUpdate(requestingAgent, group, "UPDATE");
            }

            public void Delete(UUI requestingAgent, GroupInfo group)
            {
                throw new NotImplementedException();
            }

            public UGI this[UUI requestingAgent, UUID groupID]
            {
                get 
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["GroupID"] = (string)groupID;
                    post["RequestingAgentID"] = (string)requestingAgent.ID;
                    post["METHOD"] = "GETGROUP";
                    Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                    if (!m.ContainsKey("RESULT"))
                    {
                        throw new KeyNotFoundException();
                    }
                    if (m["RESULT"].ToString() == "NULL")
                    {
                        throw new KeyNotFoundException();
                    }

                    return m["RESULT"].ToGroup(m_GroupServiceURI).ID;
                }
            }

            public GroupInfo this[UUI requestingAgent, UGI group]
            {
                get 
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["GroupID"] = (string)group.ID;
                    post["RequestingAgentID"] = (string)requestingAgent.ID;
                    post["METHOD"] = "GETGROUP";
                    Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                    if (!m.ContainsKey("RESULT"))
                    {
                        throw new KeyNotFoundException();
                    }
                    if (m["RESULT"].ToString() == "NULL")
                    {
                        throw new KeyNotFoundException();
                    }

                    return m["RESULT"].ToGroup(m_GroupServiceURI);
                }
            }

            public GroupInfo this[UUI requestingAgent, string groupName]
            {
                get 
                {
                    Dictionary<string, string> post = new Dictionary<string, string>();
                    post["Name"] = groupName;
                    post["RequestingAgentID"] = (string)requestingAgent.ID;
                    post["METHOD"] = "GETGROUP";
                    Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                    if (!m.ContainsKey("RESULT"))
                    {
                        throw new KeyNotFoundException();
                    }
                    if (m["RESULT"].ToString() == "NULL")
                    {
                        throw new KeyNotFoundException();
                    }

                    return m["RESULT"].ToGroup(m_GroupServiceURI);
                }
            }

            public List<DirGroupInfo> GetGroupsByName(UUI requestingAgent, string query)
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["Query"] = query;
                post["RequestingAgentID"] = (string)requestingAgent.ID;
                post["METHOD"] = "FINDGROUPS";
                Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_Uri, null, post, false, TimeoutMs));
                if (!m.ContainsKey("RESULT"))
                {
                    throw new KeyNotFoundException();
                }
                if (m["RESULT"].ToString() == "NULL")
                {
                    throw new KeyNotFoundException();
                }

                List<DirGroupInfo> dirgroups = new List<DirGroupInfo>();
                foreach (IValue iv in ((Map)m["RESULT"]).Values)
                {
                    if (iv is Map)
                    {
                        dirgroups.Add(iv.ToDirGroupInfo());
                    }
                }

                return dirgroups;
            }
        }
    }
}
