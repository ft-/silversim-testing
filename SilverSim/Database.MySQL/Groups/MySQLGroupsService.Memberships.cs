// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupMembershipsInterface
    {
        GroupMembership MembershipFromReader(MySqlDataReader reader, UUI requestingAgent)
        {
            GroupMembership membership = new GroupMembership();
            membership.IsAcceptNotices = reader.GetBool("AcceptNotices");
            membership.Contribution = reader.GetInt32("Contribution");
            membership.Group.ID = reader.GetUUID("GroupID");
            membership.GroupInsigniaID = reader.GetUUID("InsigniaID");
            membership.GroupPowers = reader.GetEnum<GroupPowers>("RolePowers");
            membership.GroupTitle = reader.GetString("RoleTitle");
            membership.IsListInProfile = reader.GetBool("ListInProfile");
            membership.Principal.ID = reader.GetUUID("PrincipalID");
            membership.Group = ResolveName(requestingAgent, membership.Group);
            membership.Principal = ResolveName(membership.Principal);

            membership.IsAllowPublish = reader.GetBool("AllowPublish");
            membership.Charter = reader.GetString("Charter");
            membership.ActiveRoleID = reader.GetUUID("ActiveRoleID");
            membership.Founder.ID = reader.GetUUID("FounderID");
            membership.Founder = ResolveName(membership.Founder);
            membership.AccessToken = reader.GetString("AccessToken");
            membership.IsMaturePublish = reader.GetBool("MaturePublish");
            membership.IsOpenEnrollment = reader.GetBool("OpenEnrollment");
            membership.MembershipFee = reader.GetInt32("MembershipFee");
            membership.IsShownInList = reader.GetBool("ShowInList");

            return membership;
        }

        List<GroupMembership> IGroupMembershipsInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                List<GroupMembership> memberships = new List<GroupMembership>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(
                            "SELECT g.*, m.AccessToken as AccessToken, m.SelectedRoleID AS ActiveRoleID, m.PrincipalID, m.SelectedRoleID, m.Contribution, m.ListInProfile, m.AcceptNotices, m.AccessToken, " + 
                            "r.RoleID, r.Name AS RoleName, r.Description AS RoleDescription, r.Title as RoleTitle, r.Powers as RolePowers, " +
                            RCountQuery + "," + MCountQuery + " FROM (groupmemberships AS m INNER JOIN groups AS g ON m.GroupID = g.GroupID) " +
                            "INNER JOIN grouproles AS r ON m.SelectedRoleID = r.RoleID " +
                            "WHERE m.PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                memberships.Add(MembershipFromReader(reader, requestingAgent));
                            }
                        }
                    }
                }
                return memberships;
            }
        }

        GroupMembership IGroupMembershipsInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                GroupMembership gmem;
                if(!Memberships.TryGetValue(requestingAgent, group, principal,out gmem))
                {
                    throw new KeyNotFoundException();
                }
                return gmem;
            }
        }

        bool IGroupMembershipsInterface.ContainsKey(UUI requestingAgent, UGI group, UUI principal)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(
                        "SELECT g.*, m.PrincipalID, m.SelectedRoleID AS ActiveRoleID, m.Contribution, m.ListInProfile, m.AcceptNotices, m.AccessToken, " +
                        "r.RoleID, r.Name AS RoleName, r.Description AS RoleDescription, r.Title as RoleTitle, r.Powers as RolePowers, " +
                        RCountQuery + "," + MCountQuery + " FROM (groupmemberships AS m INNER JOIN groups AS g ON m.GroupID = g.GroupID) " +
                        "INNER JOIN grouproles AS r ON m.SelectedRoleID = r.RoleID " +
                        "WHERE m.PrincipalID LIKE ?principalid", conn))
                {
                    cmd.Parameters.AddParameter("?principalid", principal.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        bool IGroupMembershipsInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMembership gmem)
        {
            gmem = null;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(
                        "SELECT g.*, m.PrincipalID, m.SelectedRoleID AS ActiveRoleID, m.Contribution, m.ListInProfile, m.AcceptNotices, m.AccessToken, " +
                        "r.RoleID, r.Name AS RoleName, r.Description AS RoleDescription, r.Title as RoleTitle, r.Powers as RolePowers, " +
                        RCountQuery + "," + MCountQuery + " FROM (groupmemberships AS m INNER JOIN groups AS g ON m.GroupID = g.GroupID) " +
                        "INNER JOIN grouproles AS r ON m.SelectedRoleID = r.RoleID " +
                        "WHERE m.PrincipalID LIKE ?principalid", conn))
                {
                    cmd.Parameters.AddParameter("?principalid", principal.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            gmem = MembershipFromReader(reader, requestingAgent);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
