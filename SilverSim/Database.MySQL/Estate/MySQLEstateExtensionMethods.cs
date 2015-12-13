// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types.Estate;

namespace SilverSim.Database.MySQL.Estate
{
    public static class MySQLEstateExtensionMethods
    {
        public static EstateInfo ToEstateInfo(this MySqlDataReader reader)
        {
            EstateInfo ei = new EstateInfo();
            ei.ID = (uint)reader["ID"];
            ei.Name = (string)reader["Name"];
            ei.Owner.ID = reader.GetUUID("OwnerID");
            ei.Flags = (RegionOptionFlags)(uint)reader["Flags"];
            ei.PricePerMeter = (int)reader["PricePerMeter"];
            ei.BillableFactor = (double)reader["BillableFactor"];
            ei.SunPosition = (double)reader["SunPosition"];
            ei.AbuseEmail = (string)reader["AbuseEmail"];
            ei.CovenantID = reader.GetUUID("CovenantID");
            ei.CovenantTimestamp = reader.GetDate("CovenantTimestamp");
            ei.UseGlobalTime = (uint)reader["UseGlobalTime"] != 0;

            return ei;
        }
    }
}
