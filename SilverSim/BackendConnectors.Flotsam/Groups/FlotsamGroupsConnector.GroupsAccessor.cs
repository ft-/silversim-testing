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
        class GroupsAccessor : FlotsamGroupsCommonConnector, IGroupsInterface
        {
            public GroupsAccessor(string uri)
                : base(uri)
            {
            }

            public GroupInfo Create(UUI requestingAgent, GroupInfo group)
            {
                Map m = new Map();
                m.Add("GroupID", group.ID.ID);
                m.Add("Name", group.ID.GroupName);
                m.Add("Charter", group.Charter);
                m.Add("InsigniaID", group.InsigniaID);
                m.Add("FounderID", group.Founder.ID);
                m.Add("MembershipFee", group.MembershipFee);
                m.Add("OpenEnrollment", group.IsOpenEnrollment.ToString());
                m.Add("ShowInList", group.IsShownInList ? 1 : 0);
                m.Add("AllowPublish", group.IsAllowPublish ? 1 : 0);
                m.Add("MaturePublish", group.IsMaturePublish ? 1 : 0);
                m.Add("OwnerRoleID", group.OwnerRoleID);
                m.Add("EveryonePowers", ((ulong)GroupPowers.DefaultEveryonePowers).ToString());
                m.Add("OwnersPowers", ((ulong)GroupPowers.OwnerPowers).ToString());
                return FlotsamXmlRpcCall(requestingAgent, "groups.createGroup", m).ToGroupInfo();
            }

            public GroupInfo Update(UUI requestingAgent, GroupInfo group)
            {
                Map m = new Map();
                m.Add("GroupID", group.ID.ID);
                m.Add("Charter", group.Charter);
                m.Add("InsigniaID", group.InsigniaID);
                m.Add("MembershipFee", group.MembershipFee);
                m.Add("OpenEnrollment", group.IsOpenEnrollment.ToString());
                m.Add("ShowInList", group.IsShownInList ? 1 : 0);
                m.Add("AllowPublish", group.IsAllowPublish ? 1 : 0);
                m.Add("MaturePublish", group.IsMaturePublish ? 1 : 0);
                FlotsamXmlRpcCall(requestingAgent, "groups.updateGroup", m).ToGroupInfo();
                return this[requestingAgent, group.ID];
            }

            public void Delete(UUI requestingAgent, GroupInfo group)
            {
                throw new NotImplementedException();
            }

            public UGI this[UUI requestingAgent, UUID groupID]
            {
                get
                {
                    Map m = new Map();
                    m.Add("GroupID", groupID);
                    return FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroup", m).ToGroupInfo().ID;
                }
            }

            public GroupInfo this[UUI requestingAgent, UGI group]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("GroupID", group.ID);
                    return FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroup", m).ToGroupInfo();
                }
            }

            public GroupInfo this[UUI requestingAgent, string groupName]
            {
                get 
                {
                    Map m = new Map();
                    m.Add("Name", groupName);
                    return FlotsamXmlRpcGetCall(requestingAgent, "groups.getGroup", m).ToGroupInfo();
                }
            }

            public List<DirGroupInfo> GetGroupsByName(UUI requestingAgent, string query)
            {
                Map m = new Map();
                m.Add("Search", query);
                AnArray results = (AnArray)FlotsamXmlRpcCall(requestingAgent, "groups.findGroups", m);

                List<DirGroupInfo> groups = new List<DirGroupInfo>();
                foreach(IValue iv in results)
                {
                    if (iv is Map)
                    {
                        groups.Add(iv.ToDirGroupInfo());
                    }
                }

                return groups;
            }
        }
    }
}
