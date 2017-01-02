// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupMembersInterface
    {
        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                List<GroupMember> members = new List<GroupMember>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT m.* FROM groupmemberships AS m WHERE m.PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                GroupMember gmem = reader.ToGroupMember();
                                gmem.Group = ResolveName(requestingAgent, gmem.Group);
                                gmem.Principal = ResolveName(gmem.Principal);
                                members.Add(gmem);
                            }
                        }
                    }
                }
                return members;
            }
        }

        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                List<GroupMember> members = new List<GroupMember>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT m.* FROM groupmemberships AS m WHERE m.GroupID LIKE ?groupid", conn))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupMember gmem = reader.ToGroupMember();
                                gmem.Group = ResolveName(requestingAgent, gmem.Group);
                                gmem.Principal = ResolveName(gmem.Principal);
                                members.Add(gmem);
                            }
                        }
                    }
                }
                return members;
            }
        }

        GroupMember IGroupMembersInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                GroupMember gmem;
                if(!Members.TryGetValue(requestingAgent, group, principal, out gmem))
                {
                    throw new KeyNotFoundException();
                }
                return gmem;
            }
        }

        GroupMember IGroupMembersInterface.Add(UUI requestingAgent, UGI group, UUI principal, UUID roleID, string accessToken)
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("GroupID", group.ID);
            vals.Add("PrincipalID", principal.ID);
            vals.Add("SelectedRoleID", roleID);
            vals.Add("AccessToken", accessToken);

            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("groupmemberships", vals);
            }

            GroupMember mem = new GroupMember();
            mem.Principal = principal;
            mem.Group = group;
            mem.IsAcceptNotices = true;
            mem.IsListInProfile = true;
            mem.AccessToken = accessToken;
            mem.SelectedRoleID = roleID;
            return mem;
        }

        bool IGroupMembersInterface.ContainsKey(UUI requestingAgent, UGI group, UUI principal)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT GroupID FROM groupmemberships WHERE GroupID LIKE ?groupid AND PrincipalID LIKE ?principalid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    cmd.Parameters.AddParameter("?principalid", principal.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        void IGroupMembersInterface.Delete(UUI requestingAgent, UGI group, UUI principal)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM groupmemberships WHERE GroupID LIKE ?groupid AND PrincipalID LIKE ?principalid", conn))
                {
                    cmd.Parameters.AddParameter("?principalid", principal.ID);
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    if(cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        void IGroupMembersInterface.SetContribution(UUI requestingagent, UGI group, UUI principal, int contribution)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE groupmemberships SET Contribution=?contribution WHERE GroupID LIKE ?groupid AND PrincipalID LIKE ?principalid", conn))
                {
                    cmd.Parameters.AddParameter("?contribution", contribution);
                    cmd.Parameters.AddParameter("?principalid", principal.ID);
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    if(cmd.ExecuteNonQuery() <1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        bool IGroupMembersInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMember gmem)
        {
            gmem = null;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupmemberships WHERE GroupID LIKE ?groupid AND PrincipalID LIKE ?principalid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    cmd.Parameters.AddParameter("?principalid", principal.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            gmem = reader.ToGroupMember();
                            gmem.Group = ResolveName(requestingAgent, gmem.Group);
                            gmem.Principal = ResolveName(gmem.Principal);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        void IGroupMembersInterface.Update(UUI requestingagent, UGI group, UUI principal, bool acceptNotices, bool listInProfile)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE groupmemberships SET AcceptNotices=?acceptnotices, ListInProfile=?listinprofile WHERE GroupID LIKE ?groupid AND PrincipalID LIKE ?principalid", conn))
                {
                    cmd.Parameters.AddParameter("?acceptnotices", acceptNotices);
                    cmd.Parameters.AddParameter("?listinprofile", listInProfile);
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    cmd.Parameters.AddParameter("?principalid", principal.ID);
                    if(cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }
    }
}
