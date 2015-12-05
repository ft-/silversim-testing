// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Asset
{
    public class MySQLAssetDataService : AssetDataServiceInterface
    {
        readonly string m_ConnectionString;
        public MySQLAssetDataService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        #region Accessor
        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        public override Stream this[UUID key]
        {
            get
            {
                Stream s;
                if(!TryGetValue(key, out s))
                {
                    throw new AssetNotFoundException(key);
                }
                return s;
            }
        }

        public override bool TryGetValue(UUID key, out Stream s)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT data FROM assets WHERE id=?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", key.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            s = new MemoryStream((byte[])dbReader["data"]);
                            return true;
                        }
                    }
                }
            }

            s = null;
            return false;
        }
        #endregion
    }
}
