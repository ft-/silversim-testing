// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using MySql.Data.MySqlClient;
using System;
using System.IO;

namespace SilverSim.Database.MySQL.Asset.Deduplication
{
    public class MySQLDedupAssetDataService : AssetDataServiceInterface
    {
        private string m_ConnectionString;
        public MySQLDedupAssetDataService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        #region Accessor
        public override Stream this[UUID key]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT data FROM assetrefs INNER JOIN assetdata ON assetrefs.hash LIKE assetdata.hash AND assetrefs.assetType = assetdata.assetType WHERE id=?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", key);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return new MemoryStream((byte[])dbReader["data"]);
                            }
                        }
                    }
                }
                throw new AssetNotFound(key);
            }
        }
        #endregion
    }
}
