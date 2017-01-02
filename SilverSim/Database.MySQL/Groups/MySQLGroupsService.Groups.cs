// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupsInterface
    {
        GroupInfo IGroupsInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                GroupInfo groupInfo;
                if (!Groups.TryGetValue(requestingAgent, group, out groupInfo))
                {
                    throw new KeyNotFoundException();
                }
                return groupInfo;
            }
        }

        GroupInfo IGroupsInterface.this[UUI requestingAgent, string groupName]
        {
            get
            {
                GroupInfo groupInfo;
                if (!Groups.TryGetValue(requestingAgent, groupName, out groupInfo))
                {
                    throw new KeyNotFoundException();
                }
                return groupInfo;
            }
        }

        UGI IGroupsInterface.this[UUI requestingAgent, UUID groupID]
        {
            get
            {
                UGI ugi;
                if(!Groups.TryGetValue(requestingAgent, groupID, out ugi))
                {
                    throw new KeyNotFoundException();
                }
                return ugi;
            }
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, string groupName)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT GroupID FROM groups WHERE `Name` LIKE ?groupname", conn))
                {
                    cmd.Parameters.AddParameter("?groupname", groupName);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UGI group)
        {
            return Groups.ContainsKey(requestingAgent, group.ID);
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UUID groupID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT GroupID FROM groups WHERE GroupID LIKE ?groupid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", groupID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        GroupInfo IGroupsInterface.Create(UUI requestingAgent, GroupInfo group)
        {
            throw new NotImplementedException();
        }

        void IGroupsInterface.Delete(UUI requestingAgent, UGI group)
        {
            string[] tablenames = new string[] { "grouproles", "grouprolememberships", "groupnotices", "groupmemberships", "groupinvites", "groups" };
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction(delegate ()
                {
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM activegroup WHERE ActiveGroupID LIKE ?groupid", conn))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (string table in tablenames)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + table + " WHERE GroupID LIKE ?groupid", conn))
                        {
                            cmd.Parameters.AddParameter("?groupid", group.ID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
        }

        List<DirGroupInfo> IGroupsInterface.GetGroupsByName(UUI requestingAgent, string query)
        {
            List<DirGroupInfo> groups = new List<DirGroupInfo>();
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT g.GroupID, g.`Name`, g.Location, " + GCountQuery + " FROM groups AS g WHERE g.Name LIKE ?value", conn))
                {
                    cmd.Parameters.AddParameter("?value", "%" + query + "%");
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            DirGroupInfo info = new DirGroupInfo();
                            info.ID.ID = reader.GetUUID("GroupID");
                            info.ID.GroupName = reader.GetString("Name");
                            string uri = reader.GetString("Location");
                            if (!string.IsNullOrEmpty(uri))
                            {
                                info.ID.HomeURI = new Uri(uri, UriKind.Absolute);
                            }
                            info.MemberCount = reader.GetInt32("MemberCount");
                            groups.Add(info);
                        }
                    }
                }
            }
            return groups;
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, string groupName, out GroupInfo groupInfo)
        {
            groupInfo = null;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT g.*, " + GCountQuery + " FROM groups AS g WHERE g.`Name` LIKE ?groupname", conn))
                {
                    cmd.Parameters.AddParameter("?groupname", groupName);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            groupInfo = reader.ToGroupInfo();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UGI group, out GroupInfo groupInfo)
        {
            groupInfo = null;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT g.*, " + GCountQuery + " FROM groups AS g WHERE g.GroupID LIKE ?groupid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            groupInfo = reader.ToGroupInfo();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UUID groupID, out UGI ugi)
        {
            ugi = default(UGI);
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT `Name`, Location FROM groups WHERE GroupID LIKE ?groupid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", groupID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ugi = new UGI();
                            ugi.ID = groupID;
                            string uri = reader.GetString("Location");
                            if(!string.IsNullOrEmpty(uri))
                            {
                                ugi.HomeURI = new Uri(uri, UriKind.Absolute);
                            }
                            ugi.GroupName = reader.GetString("Name");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        GroupInfo IGroupsInterface.Update(UUI requestingAgent, GroupInfo group)
        {
            throw new NotImplementedException();
        }
    }
}
