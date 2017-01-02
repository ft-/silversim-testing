﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupInvitesInterface
    {
        List<GroupInvite> IGroupInvitesInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * from groupinvites WHERE PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            List<GroupInvite> invites = new List<GroupInvite>();
                            while(reader.Read())
                            {
                                GroupInvite invite = reader.ToGroupInvite();
                                invite.Principal = ResolveName(invite.Principal);
                                invite.Group = ResolveName(requestingAgent, invite.Group);
                                invites.Add(invite);
                            }
                            return invites;
                        }
                    }
                }
            }
        }

        GroupInvite IGroupInvitesInterface.this[UUI requestingAgent, UUID groupInviteID]
        {
            get
            {
                GroupInvite invite;
                if(!Invites.TryGetValue(requestingAgent, groupInviteID, out invite))
                {
                    throw new KeyNotFoundException();
                }
                return invite;
            }
        }

        List<GroupInvite> IGroupInvitesInterface.this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * from groupinvites WHERE PrincipalID LIKE ?principalid AND GroupID LIKE ?groupid AND RoleID LIKE ?roleid", conn))
                    {
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        cmd.Parameters.AddParameter("?roleid", roleID);
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            List<GroupInvite> invites = new List<GroupInvite>();
                            while (reader.Read())
                            {
                                GroupInvite invite = reader.ToGroupInvite();
                                invite.Principal = ResolveName(invite.Principal);
                                invite.Group = ResolveName(requestingAgent, invite.Group);
                                invites.Add(invite);
                            }
                            return invites;
                        }
                    }
                }
            }
        }

        void IGroupInvitesInterface.Add(UUI requestingAgent, GroupInvite invite)
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("InviteID", invite.ID);
            vals.Add("GroupID", invite.Group.ID);
            vals.Add("RoleID", invite.RoleID);
            vals.Add("PrincipalID", invite.Principal.ID);
            vals.Add("Timestamp", invite.Timestamp);
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("groupinvites", vals);
            }
        }

        bool IGroupInvitesInterface.ContainsKey(UUI requestingAgent, UUID groupInviteID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT InviteID from groupinvites WHERE InviteID LIKE ?inviteid", conn))
                {
                    cmd.Parameters.AddParameter("?inviteid", groupInviteID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        void IGroupInvitesInterface.Delete(UUI requestingAgent, UUID inviteID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM groupinvites WHERE InviteID LIKE ?inviteid", conn))
                {
                    cmd.Parameters.AddParameter("?inviteid", inviteID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        List<GroupInvite> IGroupInvitesInterface.GetByGroup(UUI requestingAgent, UGI group)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * from groupinvites WHERE GroupID LIKE ?groupid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<GroupInvite> invites = new List<GroupInvite>();
                        while (reader.Read())
                        {
                            GroupInvite invite = reader.ToGroupInvite();
                            invite.Principal = ResolveName(invite.Principal);
                            invite.Group = ResolveName(requestingAgent, invite.Group);
                            invites.Add(invite);
                        }
                        return invites;
                    }
                }
            }
        }

        bool IGroupInvitesInterface.TryGetValue(UUI requestingAgent, UUID groupInviteID, out GroupInvite ginvite)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * from groupinvites WHERE InviteID LIKE ?inviteid", conn))
                {
                    cmd.Parameters.AddParameter("?inviteid", groupInviteID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ginvite = reader.ToGroupInvite();
                            ginvite.Principal = ResolveName(ginvite.Principal);
                            ginvite.Group = ResolveName(requestingAgent, ginvite.Group);
                            return true;
                        }
                    }
                }
            }
            ginvite = null;
            return false;
        }
    }
}
