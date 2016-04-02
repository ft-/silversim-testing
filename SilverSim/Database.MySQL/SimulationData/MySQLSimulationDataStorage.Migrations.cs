// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Database.MySQL._Migration;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage
    {
        public void ProcessMigrations()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(Migrations_Regions, m_Log);
                conn.MigrateTables(Migrations_Parcels, m_Log);
                conn.MigrateTables(Migrations_Objects, m_Log);
            }
        }
    }
}
