// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using System;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupSelectInterface
    {
        UGI IGroupSelectInterface.this[UUI requestingAgent, UUI principalID]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ActiveGroupID FROM activegroup WHERE Principal LIKE ?principal", conn))
                    {
                        cmd.Parameters.AddParameter("?principal", principalID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                return new UGI(reader.GetUUID("ActiveGroupID"));
                            }
                        }
                    }
                }
                return UGI.Unknown;
            }

            set
            {
                if(Members.ContainsKey(requestingAgent, value, principalID))
                {
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("INSERT INTO activegroup (Principal, ActiveGroupID) VALUES (?principal, ?activegroupid) ON DUPLICATE KEY UPDATE ActiveGroupID=?activegroupid", conn))
                        {
                            cmd.Parameters.AddParameter("?principal", principalID);
                            cmd.Parameters.AddParameter("?activegroupid", value.ID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /* get/set active role id */
        UUID IGroupSelectInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                UUID id;
                if(!ActiveGroup.TryGetValue(requestingAgent, group, principal, out id))
                {
                    id = UUID.Zero;
                }
                return id;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        bool IGroupSelectInterface.TryGetValue(UUI requestingAgent, UUI principalID, out UGI ugi)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ActiveGroupID FROM activegroup WHERE Principal LIKE ?principal", conn))
                {
                    cmd.Parameters.AddParameter("?principal", principalID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ugi = new UGI(reader.GetUUID("ActiveGroupID"));
                            return true;
                        }
                    }
                }
            }

            ugi = UGI.Unknown;
            return false;
        }

        bool IGroupSelectInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out UUID id)
        {
            throw new NotImplementedException();
        }
    }
}
