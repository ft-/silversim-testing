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

namespace SilverSim.Database.MySQL.Groups
{
    public partial class MySQLGroupsService : GroupsServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GROUPS SERVICE");

        public override IGroupSelectInterface ActiveGroup
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IActiveGroupMembershipInterface ActiveMembership
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IGroupsInterface Groups
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IGroupInvitesInterface Invites
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IGroupMembersInterface Members
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IGroupMembershipsInterface Memberships
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IGroupNoticesInterface Notices
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IGroupRolemembersInterface Rolemembers
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IGroupRolesInterface Roles
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override GroupPowers GetAgentPowers(UGI group, UUI agent)
        {
            throw new NotImplementedException();
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
