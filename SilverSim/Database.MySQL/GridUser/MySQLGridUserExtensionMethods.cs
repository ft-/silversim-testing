// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Types.GridUser;

namespace SilverSim.Database.MySQL.GridUser
{
    public static class MySQLGridUserExtensionMethods
    {
        public static GridUserInfo ToGridUser(this MySqlDataReader reader)
        {
            GridUserInfo info = new GridUserInfo();

            info.User.ID = reader.GetUUID("ID");
            info.HomeRegionID = reader.GetUUID("HomeRegionID");
            info.HomeLookAt = reader.GetVector3("HomeLookAt");
            info.HomePosition = reader.GetVector3("HomePosition");
            info.LastRegionID = reader.GetUUID("LastRegionID");
            info.LastLookAt = reader.GetVector3("LastLookAt");
            info.LastPosition = reader.GetVector3("LastPosition");
            info.IsOnline = reader.GetBool("IsOnline");
            info.LastLogin = reader.GetDate("LastLogin");
            info.LastLogout = reader.GetDate("LastLogout");

            return info;
        }
    }
}
