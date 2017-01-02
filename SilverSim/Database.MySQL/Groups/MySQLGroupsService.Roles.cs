// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupRolesInterface
    {
        List<GroupRole> IGroupRolesInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                List<GroupRole> roles = new List<GroupRole>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT r.*," + RCountQuery + " FROM grouproles AS r WHERE r.GroupID LIKE ?groupid"))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                GroupRole role = reader.ToGroupRole();
                                role.Group = ResolveName(requestingAgent, role.Group);
                                roles.Add(role);
                            }
                        }
                    }
                }
                return roles;
            }
        }

        List<GroupRole> IGroupRolesInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                List<GroupRole> roles = new List<GroupRole>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT r.*," + RCountQuery + " FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID AND r.GroupID AND rm.RoleID LIKE r.RoleID WHERE r.GroupID LIKE ?groupid AND rm.PrincipalID LIKE ?principalid"))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRole role = reader.ToGroupRole();
                                role.Group = ResolveName(requestingAgent, role.Group);
                                roles.Add(role);
                            }
                        }
                    }
                }
                return roles;
            }
        }

        GroupRole IGroupRolesInterface.this[UUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                GroupRole role;
                if(!Roles.TryGetValue(requestingAgent, group, roleID, out role))
                {
                    throw new KeyNotFoundException();
                }
                return role;
            }
        }

        void IGroupRolesInterface.Add(UUI requestingAgent, GroupRole role)
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("GroupID", role.Group.ID);
            vals.Add("RoleID", role.ID);
            vals.Add("Name", role.Name);
            vals.Add("Description", role.Description);
            vals.Add("Title", role.Title);
            vals.Add("Powers", role.Powers);
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("grouproles", vals);
            }
        }

        bool IGroupRolesInterface.ContainsKey(UUI requestingAgent, UGI group, UUID roleID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT r.GroupID FROM grouproles AS r WHERE r.GroupID LIKE ?groupid AND r.RoleID LIKE ?roleid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    cmd.Parameters.AddParameter("?roleid", roleID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        void IGroupRolesInterface.Delete(UUI requestingAgent, UGI group, UUID roleID)
        {
            string[] tablenames = new string[] { "groupinvites", "grouprolememberships", "grouproles" };

            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.InsideTransaction(delegate ()
                {
                    using (MySqlCommand cmd = new MySqlCommand("UPDATE groupmemberships SET SelectedRoleID=?zeroid WHERE SelectedRoleID LIKE ?roleid", conn))
                    {
                        cmd.Parameters.AddParameter("?zeroid", UUID.Zero);
                        cmd.Parameters.AddParameter("?roleid", roleID);
                        cmd.ExecuteNonQuery();
                    }

                    foreach(string table in tablenames)
                    {
                        using(MySqlCommand cmd = new MySqlCommand("DELETE FROM " + table + " WHERE GroupID LIKE ?groupid AND RoleID LIKE ?roleid",conn))
                        {
                            cmd.Parameters.AddParameter("?groupid", group.ID);
                            cmd.Parameters.AddParameter("?roleid", roleID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
        }

        bool IGroupRolesInterface.TryGetValue(UUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole)
        {
            groupRole = null;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT r.*, " + RCountQuery + " FROM grouproles AS r WHERE r.GroupID LIKE ?groupid AND r.RoleID LIKE ?roleid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    cmd.Parameters.AddParameter("?roleid", roleID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            groupRole = reader.ToGroupRole();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        void IGroupRolesInterface.Update(UUI requestingAgent, GroupRole role)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE grouproles SET Name=?name, Description=?description, Title=?title,Powers=?powers WHERE GroupID LIKE ?groupid AND RoleID LIKE ?roleid", conn))
                {
                    cmd.Parameters.AddParameter("?name", role.Name);
                    cmd.Parameters.AddParameter("?description", role.Description);
                    cmd.Parameters.AddParameter("?title", role.Title);
                    cmd.Parameters.AddParameter("?powers", role.Powers);
                    cmd.Parameters.AddParameter("?groupid", role.Group.ID);
                    cmd.Parameters.AddParameter("?roleid", role.ID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
