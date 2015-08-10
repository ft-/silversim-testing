// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using SilverSim.Types.GridUser;
using SilverSim.Types;
using SilverSim.Types.Account;

namespace SilverSim.Database.MySQL.UserAccounts
{
    public static class MySQLUserAccountExtensionMethods
    {
        public static UserAccount ToUserAccount(this MySqlDataReader reader)
        {
            UserAccount info = new UserAccount();

            info.Principal.ID = reader.GetUUID("ID");
            info.Principal.FirstName = (string)reader["FirstName"];
            info.Principal.LastName = (string)reader["LastName"];
            info.Principal.HomeURI = null;
            info.Principal.IsAuthoritative = true;
            info.ScopeID = reader.GetUUID("ScopeID");
            info.Email = (string)reader["Email"];
            info.Created = reader.GetDate("Created");
            info.UserLevel = (int)reader["UserLevel"];
            info.UserFlags = (int)reader["UserFlags"];
            info.UserTitle = (string)reader["UserTitle"];
            info.IsLocalToGrid = true;

            return info;
        }
    }
}
