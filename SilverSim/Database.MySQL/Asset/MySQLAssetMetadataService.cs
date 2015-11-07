// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using MySql.Data.MySqlClient;
using System;

namespace SilverSim.Database.MySQL.Asset
{
    public class MySQLAssetMetadataService : AssetMetadataServiceInterface
    {
        readonly string m_ConnectionString;
        public MySQLAssetMetadataService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        #region Accessor
        public override AssetMetadata this[UUID key]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM assets WHERE id=?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", key.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                AssetMetadata asset = new AssetMetadata();
                                asset.ID = dbReader.GetUUID("id");
                                asset.Type = (AssetType)(int)dbReader["assetType"];
                                asset.Name = (string)dbReader["name"];
                                asset.Creator.ID = dbReader.GetUUID("CreatorID");
                                asset.CreateTime = dbReader.GetDate("create_time");
                                asset.AccessTime = dbReader.GetDate("access_time");
                                asset.Flags = dbReader.GetAssetFlags("asset_flags");
                                asset.Temporary = dbReader.GetBoolean("temporary");
                                return asset;
                            }
                        }
                    }
                }
                throw new AssetNotFoundException(key);
            }
        }
        #endregion
    }
}
