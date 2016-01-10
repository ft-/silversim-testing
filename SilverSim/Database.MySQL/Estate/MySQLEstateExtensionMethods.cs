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
            ei.ID = reader.GetUInt32("ID");
            ei.Name = reader.GetString("Name");
            ei.Owner = reader.GetUUI("Owner");
            ei.Flags = reader.GetEnum<RegionOptionFlags>("Flags");
            ei.PricePerMeter = reader.GetInt32("PricePerMeter");
            ei.BillableFactor = reader.GetDouble("BillableFactor");
            ei.SunPosition = reader.GetDouble("SunPosition");
            ei.AbuseEmail = reader.GetString("AbuseEmail");
            ei.CovenantID = reader.GetUUID("CovenantID");
            ei.CovenantTimestamp = reader.GetDate("CovenantTimestamp");
            ei.UseGlobalTime = reader.GetBool("UseGlobalTime");

            return ei;
        }
    }
}
