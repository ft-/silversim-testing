/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using MySql.Data.MySqlClient;
using System;

namespace SilverSim.Database.MySQL.Asset
{
    public class MySQLAssetMetadataService : AssetMetadataServiceInterface
    {
        private string m_ConnectionString;
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
                        cmd.Parameters.AddWithValue("?id", key);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                AssetMetadata asset = new AssetMetadata();
                                asset.ID = dbReader.GetUUID("id");
                                asset.Type = (AssetType)dbReader["assetType"];
                                asset.Name = (string)dbReader["name"];
                                asset.Description = (string)dbReader["description"];
                                asset.Creator.ID = dbReader.GetUUID("CreatorID");
                                asset.CreateTime = Date.UnixTimeToDateTime(ulong.Parse(dbReader["create_time"].ToString()));
                                asset.AccessTime = Date.UnixTimeToDateTime(ulong.Parse(dbReader["access_time"].ToString()));
                                uint.TryParse(dbReader["asset_flags"].ToString(), out asset.Flags);
                                Boolean.TryParse(dbReader["temporary"].ToString(), out asset.Temporary);
                                Boolean.TryParse(dbReader["local"].ToString(), out asset.Local);
                                return asset;
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
