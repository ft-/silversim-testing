// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using Nini.Config;
using log4net;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Account;
using System.Collections.Generic;
using SilverSim.ServiceInterfaces.AvatarName;
using MySql.Data.MySqlClient;

namespace SilverSim.Database.MySQL.Groups
{
    public partial class MySQLGroupsService : GroupsServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GROUPS SERVICE");
        private readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();

        const string GCountQuery = "(SELECT COUNT(m.PrincipalID) FROM groupmemberships AS m WHERE m.GroupID LIKE g.GroupID) AS MemberCount," +
				"(SELECT COUNT(r.RoleID) FROM grouproles AS r WHERE r.GroupID LIKE g.GroupID) AS RoleCount";

        const string MCountQuery = "(SELECT COUNT(xr.RoleID) FROM grouproles AS xr WHERE xr.GroupID LIKE g.GroupID) AS RoleCount";

        const string RCountQuery = "(SELECT COUNT(xrm.PrincipalID) FROM grouprolemembers AS xrm WHERE xrm.RoleID LIKE r.RoleID) AS RoleMembers," +
					"(SELECT COUNT(xm.PrincipalID) FROM groupmemberships AS xm WHERE xm.GroupID LIKE r.GroupID) AS GroupMembers";

        UUI ResolveName(UUI uui)
        {
            UUI searchuui = uui;
            foreach(AvatarNameServiceInterface service in m_AvatarNameServices)
            {
                UUI resultuui;
                if (service.TryGetValue(searchuui, out resultuui))
                {
                    searchuui = resultuui;
                    if(resultuui.IsAuthoritative)
                    {
                        break;
                    }
                }
            }
            return searchuui;
        }

        public override IGroupSelectInterface ActiveGroup
        {
            get
            {
                return this;
            }
        }

        public override IActiveGroupMembershipInterface ActiveMembership
        {
            get
            {
                return this;
            }
        }

        public override IGroupsInterface Groups
        {
            get
            {
                return this;
            }
        }

        public override IGroupInvitesInterface Invites
        {
            get
            {
                return this;
            }
        }

        public override IGroupMembersInterface Members
        {
            get
            {
                return this;
            }
        }

        public override IGroupMembershipsInterface Memberships
        {
            get
            {
                return this;
            }
        }

        public override IGroupNoticesInterface Notices
        {
            get
            {
                return this;
            }
        }

        public override IGroupRolemembersInterface Rolemembers
        {
            get
            {
                return this;
            }
        }

        public override IGroupRolesInterface Roles
        {
            get
            {
                return this;
            }
        }

        bool TryGetGroupRoleRights(UUI requestingAgent, UGI group, UUID roleID, out GroupPowers powers)
        {
            powers = GroupPowers.None;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT Powers FROM grouproles AS r WHERE r.GroupID LIKE ?groupid AND r.RoleID LIKE ?grouproleid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    cmd.Parameters.AddParameter("?grouproleid", roleID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            powers = reader.GetEnum<GroupPowers>("Powers");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override GroupPowers GetAgentPowers(UGI group, UUI agent)
        {
            if(!Members.ContainsKey(agent, group, agent))
            {
                return GroupPowers.None;
            }

            GroupPowers powers;
            if (!TryGetGroupRoleRights(agent, group, UUID.Zero, out powers))
            {
                return GroupPowers.None;
            }

            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(
                    "SELECT Powers FROM roles AS r INNER JOIN " +
                    "((grouprolemembers AS rm INNER JOIN groupmembers AS m ON rm.GroupID LIKE m.GroupID AND rm.PrincipalID LIKE m.PrincipalID) ON " +
                    "r.RoleID LIKE rm.RoleID WHERE rm.GroupID LIKE ?groupid AND rm.PrincipalID LIKE ?principalid", conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            powers |= reader.GetEnum<GroupPowers>("Powers");
                        }
                    }
                }
            }
            return powers;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            throw new NotImplementedException();
        }

        readonly string m_ConnectionString;

        public MySQLGroupsService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }
    }

    [PluginName("Groups")]
    public class MySQLGroupsServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GROUPS SERVICE");
        public MySQLGroupsServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLGroupsService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
}
