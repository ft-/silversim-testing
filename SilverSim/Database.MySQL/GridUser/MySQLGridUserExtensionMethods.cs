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
            info.HomeLookAt = reader.GetStringFormattedVector("HomeLookAt");
            info.HomePosition = reader.GetStringFormattedVector("HomePosition");
            info.LastRegionID = reader.GetUUID("LastRegionID");
            info.LastLookAt = reader.GetStringFormattedVector("LastLookAt");
            info.LastPosition = reader.GetStringFormattedVector("LastPosition");
            info.IsOnline = reader.GetBoolean("IsOnline");
            info.LastLogin = reader.GetDate("LastLogin");
            info.LastLogout = reader.GetDate("LastLogout");

            return info;
        }
    }
}
