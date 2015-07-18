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
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpClient;
using SilverSim.Main.Common.Rpc;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.GroupsV2
{
    public partial class RobustGroupsConnector : GroupsServiceInterface, IPlugin
    {
        GroupsAccessor m_Groups;
        GroupRolesAccessor m_GroupRoles;
        MembersAccessor m_Members;
        MembershipsAccessor m_Memberships;
        RoleMembersAccessor m_Rolemembers;
        ActiveGroupAccessor m_ActiveGroup;
        InvitesAccessor m_Invites;
        NoticesAccessor m_Notices;
        ActiveGroupMembershipAccesor m_ActiveGroupMembership;
        int m_TimeoutMs = 20000;

        public int TimeoutMs
        {
            get
            {
                return m_TimeoutMs;
            }
            set
            {
                m_TimeoutMs = value;
                m_Groups.TimeoutMs = value;
                m_GroupRoles.TimeoutMs = value;
                m_Members.TimeoutMs = value;
                m_Memberships.TimeoutMs = value;
                m_Rolemembers.TimeoutMs = value;
                m_ActiveGroup.TimeoutMs = value;
                m_Invites.TimeoutMs = value;
                m_Notices.TimeoutMs = value;
                m_ActiveGroupMembership.TimeoutMs = value;
            }
        }

        public RobustGroupsConnector(string uri, string serviceUri)
        {
            if(!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "groups";
            m_Groups = new GroupsAccessor(uri, serviceUri);
            m_GroupRoles = new GroupRolesAccessor(uri);
            m_Members = new MembersAccessor(uri);
            m_Memberships = new MembershipsAccessor(uri);
            m_Rolemembers = new RoleMembersAccessor(uri);
            m_ActiveGroup = new ActiveGroupAccessor(uri);
            m_Invites = new InvitesAccessor(uri);
            m_Notices = new NoticesAccessor(uri);
            m_ActiveGroupMembership = new ActiveGroupMembershipAccesor(uri);
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public override IGroupsInterface Groups
        {
            get
            {
                return m_Groups;
            }
        }

        public override IGroupRolesInterface Roles
        {
            get 
            {
                return m_GroupRoles;
            }
        }

        public override IGroupMembersInterface Members
        {
            get 
            {
                return m_Members;
            }
        }

        public override IGroupMembershipsInterface Memberships
        {
            get 
            {
                return m_Memberships;
            }
        }

        public override IGroupRolemembersInterface Rolemembers
        {
            get 
            {
                return m_Rolemembers;
            }
        }

        public override IGroupSelectInterface ActiveGroup
        {
            get 
            {
                return m_ActiveGroup;
            }
        }

        public override IActiveGroupMembershipInterface ActiveMembership
        {
            get 
            {
                return m_ActiveGroupMembership;
            }
        }

        public override IGroupInvitesInterface Invites
        {
            get
            {
                return m_Invites;
            }
        }

        public override IGroupNoticesInterface Notices
        {
            get 
            {
                return m_Notices;
            }
        }

        internal static void BooleanResponseRequest(string uri, Dictionary<string, string> post, bool compressed, int timeoutms)
        {
            Map m = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(uri, null, post, compressed, timeoutms));
            if(!m.ContainsKey("RESULT"))
            {
                throw new AccessFailedException();
            }
            if(m["RESULT"].ToString().ToLower() != "true")
            {
                throw new AccessFailedException();
            }
        }
    }
}
