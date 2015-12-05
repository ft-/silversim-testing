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
                AssetMetadata metadata;
                if(!TryGetValue(key, out metadata))
                {
                    throw new AssetNotFoundException(key);
                }
                return metadata;
            }
        }

        public override bool TryGetValue(UUID key, out AssetMetadata metadata)
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
                            metadata = new AssetMetadata();
                            metadata.ID = dbReader.GetUUID("id");
                            metadata.Type = (AssetType)(int)dbReader["assetType"];
                            metadata.Name = (string)dbReader["name"];
                            metadata.Creator.ID = dbReader.GetUUID("CreatorID");
                            metadata.CreateTime = dbReader.GetDate("create_time");
                            metadata.AccessTime = dbReader.GetDate("access_time");
                            metadata.Flags = dbReader.GetAssetFlags("asset_flags");
                            metadata.Temporary = dbReader.GetBoolean("temporary");
                            return true;
                        }
                    }
                }
            }
            metadata = null;
            return false;
        }
        #endregion
    }
}
