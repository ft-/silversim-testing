// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    public partial class MySQLGroupsService : GroupsServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GROUPS SERVICE");
        private readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();

        const string GCountQuery = "(SELECT COUNT(m.PrincipalID) FROM groupmemberships AS m WHERE m.GroupID LIKE g.GroupID) AS MemberCount," +
				"(SELECT COUNT(r.RoleID) FROM grouproles AS r WHERE r.GroupID LIKE g.GroupID) AS RoleCount";

        const string MCountQuery = "(SELECT COUNT(xr.RoleID) FROM grouproles AS xr WHERE xr.GroupID LIKE g.GroupID) AS RoleCount";

        const string RCountQuery = "(SELECT COUNT(xrm.PrincipalID) FROM grouprolememberships AS xrm WHERE xrm.RoleID LIKE r.RoleID AND xrm.GroupID LIKE r.GroupID) AS RoleMembers," +
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

        UGI ResolveName(UUI requestingAgent, UGI group)
        {
            UGI resolved;
            return Groups.TryGetValue(requestingAgent, group.ID, out resolved) ? resolved : group;
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
            foreach(string name in m_AvatarNameServiceNames.Trim().Split(','))
            {
                m_AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(name));
            }
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction(delegate ()
                {
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM groupinvites WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM groupmemberships WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM activegroup WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM grouprolememberships WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                });
            }
        }

        readonly string m_ConnectionString;
        readonly string m_AvatarNameServiceNames;

        public MySQLGroupsService(IConfig ownSection)
        {
            m_ConnectionString = MySQLUtilities.BuildConnectionString(ownSection, m_Log);
            m_AvatarNameServiceNames = ownSection.GetString("AvatarNameServices", "AvatarNameStorage");
        }
    }

    [PluginName("Groups")]
    public class MySQLGroupsServiceFactory : IPluginFactory
    {
        public MySQLGroupsServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLGroupsService(ownSection);
        }
    }
}
