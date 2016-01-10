﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;

namespace SilverSim.Database.MySQL.Asset.Deduplication
{
    public class MySQLDedupAssetMetadataService : AssetMetadataServiceInterface
    {
        readonly string m_ConnectionString;
        public MySQLDedupAssetMetadataService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        #region Accessor
        public override AssetMetadata this[UUID key]
        {
            get
            {
                AssetMetadata s;
                if(!TryGetValue(key, out s))
                {
                    throw new AssetNotFoundException(key);
                }
                return s;
            }
        }

        public override bool TryGetValue(UUID key, out AssetMetadata metadata)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM assetrefs WHERE id=?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            metadata = new AssetMetadata();
                            metadata.ID = dbReader.GetUUID("id");
                            metadata.Type = dbReader.GetEnum<AssetType>("assetType");
                            metadata.Name = dbReader.GetString("name");
                            metadata.Creator = dbReader.GetUUI("CreatorID");
                            metadata.CreateTime = dbReader.GetDate("create_time");
                            metadata.AccessTime = dbReader.GetDate("access_time");
                            metadata.Flags = dbReader.GetEnum<AssetFlags>("asset_flags");
                            metadata.Temporary = dbReader.GetBool("temporary");
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
