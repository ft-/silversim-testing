﻿/*

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

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace SilverSim.Database.MySQL.Asset.Deduplication
{
    #region Service Implementation
    public class MySQLDedupAssetService : AssetServiceInterface, IDBServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL DEDUP ASSET SERVICE");

        private string m_ConnectionString;
        private MySQLDedupAssetMetadataService m_MetadataService;
        private DefaultAssetReferencesService m_ReferencesService;
        private MySQLDedupAssetDataService m_DataService;

        #region Constructor
        public MySQLDedupAssetService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_MetadataService = new MySQLDedupAssetMetadataService(connectionString);
            m_DataService = new MySQLDedupAssetDataService(connectionString);
            m_ReferencesService = new DefaultAssetReferencesService(this);
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion


        #region Hashing

        #endregion

        #region Exists methods
        public override void exists(UUID key)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT id, access_time FROM assetrefs WHERE id LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if(dbReader.Read())
                        {
                            DateTime d = Date.UnixTimeToDateTime((ulong)dbReader["access_time"]);
                            if(d - DateTime.UtcNow > TimeSpan.FromHours(1))
                            {
                                /* update access_time */
                                using(MySqlConnection uconn = new MySqlConnection(m_ConnectionString))
                                {
                                    uconn.Open();
                                    using(MySqlCommand ucmd = new MySqlCommand("UPDATE assets SET access_time = ?access WHERE id LIKE ?id", uconn))
                                    {
                                        ucmd.Parameters.AddWithValue("?access", Date.GetUnixTime());
                                        ucmd.Parameters.AddWithValue("?id", key);
                                        ucmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            return;
                        }
                    }
                }
            }
            throw new AssetNotFound(key);
        }

        public override Dictionary<UUID, bool> exists(List<UUID> assets)
        {
            Dictionary<UUID,bool> res = new Dictionary<UUID,bool>();
            if (assets.Count == 0)
                return res;

            foreach(UUID id in assets)
            {
                res[id] = false;
            }

            string ids = "'" + string.Join("','", assets) + "'";
            string sql = string.Format("SELECT id, access_time FROM assetrefs WHERE id IN ({0})", ids);

            using (MySqlConnection dbcon = new MySqlConnection(m_ConnectionString))
            {
                dbcon.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, dbcon))
                {
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            UUID id = new UUID((string)dbReader["id"]);
                            res[id] = true;
                            DateTime d = Date.UnixTimeToDateTime((ulong)dbReader["access_time"]);
                            if (d - DateTime.UtcNow > TimeSpan.FromHours(1))
                            {
                                /* update access_time */
                                using (MySqlConnection uconn = new MySqlConnection(m_ConnectionString))
                                {
                                    uconn.Open();
                                    using (MySqlCommand ucmd = new MySqlCommand("UPDATE assetrefs SET access_time = ?access WHERE id LIKE ?id", uconn))
                                    {
                                        ucmd.Parameters.AddWithValue("?access", Date.GetUnixTime());
                                        ucmd.Parameters.AddWithValue("?id", id);
                                        ucmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return res;
        }

        #endregion

        #region Accessors
        public override AssetData this[UUID key]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM assetrefs INNER JOIN assetdata ON assetrefs.hash = assetdata.hash AND assetrefs.assetType = assetdata.assetType WHERE id LIKE ?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", key);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                AssetData asset = new AssetData();
                                asset.ID = dbReader.GetUUID("id");
                                asset.Data = (byte[])dbReader["data"];
                                asset.Type = (AssetType)dbReader["assetType"];
                                asset.Name = (string)dbReader["name"];
                                asset.Description = (string)dbReader["description"];
                                asset.CreateTime = dbReader.GetDate("create_time");
                                asset.AccessTime = dbReader.GetDate("access_time");
                                asset.Creator.ID = dbReader.GetUUID("CreatorID");
                                uint.TryParse(dbReader["asset_flags"].ToString(), out asset.Flags);
                                Boolean.TryParse(dbReader["temporary"].ToString(), out asset.Temporary);
                                Boolean.TryParse(dbReader["local"].ToString(), out asset.Local);

                                if (asset.AccessTime - DateTime.UtcNow > TimeSpan.FromHours(1))
                                {
                                    /* update access_time */
                                    using (MySqlConnection uconn = new MySqlConnection(m_ConnectionString))
                                    {
                                        uconn.Open();
                                        using (MySqlCommand ucmd = new MySqlCommand("UPDATE assetrefs SET access_time = ?access WHERE id LIKE ?id", uconn))
                                        {
                                            ucmd.Parameters.AddWithValue("?access", Date.GetUnixTime());
                                            ucmd.Parameters.AddWithValue("?id", key);
                                            ucmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                                return asset;
                            }
                        }
                    }
                }
                throw new AssetNotFound(key);
            }
        }

        #endregion

        #region Metadata interface
        public override AssetMetadataServiceInterface Metadata
        {
            get
            {
                return m_MetadataService;
            }
        }
        #endregion

        #region References interface
        public override AssetReferencesServiceInterface References
        {
            get
            {
                return m_ReferencesService;
            }
        }
        #endregion

        #region Data interface
        public override AssetDataServiceInterface Data
        {
            get
            {
                return m_DataService;
            }
        }
        #endregion

        #region Store asset method
        void CheckForAssetData(string sha1, int type, UUID key)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT assetType FROM assetdata WHERE hash LIKE ?hash AND type = ?type", conn))
                {
                    cmd.Parameters.AddWithValue("?hash", sha1);
                    cmd.Parameters.AddWithValue("?type", type);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            return;
                        }
                    }
                }
            }
            throw new AssetStoreFailed(key);
        }

        public override void Store(AssetData asset)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] sha1data = sha.ComputeHash(asset.Data);
            string sha1 = Convert.ToBase64String(sha1data);
            
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();

                using (MySqlCommand cmd =
                    new MySqlCommand(
                        "INSERT INTO assetdata (hash, assetType, data)" +
                        "VALUES(?hash, ?assetType, ?data)",
                        conn))
                {
                    try
                    {
                        using (cmd)
                        {
                            // create unix epoch time
                            ulong now = Date.GetUnixTime();
                            cmd.Parameters.AddWithValue("?hash", sha1);
                            cmd.Parameters.AddWithValue("?assetType", (int)asset.Type);
                            cmd.Parameters.AddWithValue("?data", asset.Data);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                    }
                }

                CheckForAssetData(sha1, (int)asset.Type, asset.ID);

                using (MySqlCommand cmd =
                    new MySqlCommand(
                        "INSERT INTO assetrefs (id, name, description, assetType, local, temporary, create_time, access_time, asset_flags, CreatorID, hash)" +
                        "VALUES(?id, ?name, ?description, ?assetType, ?local, ?temporary, ?create_time, ?access_time, ?asset_flags, ?CreatorID, ?hash)",
                        conn))
                {
                    string assetName = asset.Name;
                    if (asset.Name.Length > MAX_ASSET_NAME)
                    {
                        assetName = asset.Name.Substring(0, MAX_ASSET_NAME);
                        m_Log.WarnFormat("Name '{0}' for asset {1} truncated from {2} to {3} characters on add",
                            asset.Name, asset.ID, asset.Name.Length, assetName.Length);
                    }

                    string assetDescription = asset.Description;
                    if (asset.Description.Length > MAX_ASSET_DESC)
                    {
                        assetDescription = asset.Description.Substring(0, MAX_ASSET_DESC);
                        m_Log.WarnFormat("Description '{0}' for asset {1} truncated from {2} to {3} characters on add",
                            asset.Description, asset.ID, asset.Description.Length, assetDescription.Length);
                    }

                    try
                    {
                        using (cmd)
                        {
                            // create unix epoch time
                            ulong now = Date.GetUnixTime();
                            cmd.Parameters.AddWithValue("?id", asset.ID);
                            cmd.Parameters.AddWithValue("?name", assetName);
                            cmd.Parameters.AddWithValue("?description", assetDescription);
                            cmd.Parameters.AddWithValue("?assetType", (int)asset.Type);
                            cmd.Parameters.AddWithValue("?local", asset.Local);
                            cmd.Parameters.AddWithValue("?temporary", asset.Temporary);
                            cmd.Parameters.AddWithValue("?create_time", now);
                            cmd.Parameters.AddWithValue("?access_time", now);
                            cmd.Parameters.AddWithValue("?CreatorID", asset.Creator.ID);
                            cmd.Parameters.AddWithValue("?asset_flags", (int)asset.Flags);
                            cmd.Parameters.AddWithValue("?hash", sha1);
                            if(1 > cmd.ExecuteNonQuery())
                            {
                                throw new AssetStoreFailed(asset.ID);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        m_Log.Error(
                            string.Format("MySQL failure creating asset {0} with name {1}.  Exception  ",
                                asset.ID, asset.Name)
                            , e);
                        throw new AssetStoreFailed(asset.ID);
                    }
                }
            }
        }
        #endregion

        #region Delete asset method
        public override void Delete(UUID id)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM assetrefs WHERE id=?id AND asset_flags <> 0", conn))
                {
                    cmd.Parameters.AddWithValue("?id", id);
                }
            }
        }
        #endregion

        #region DBServiceInterface
        public void VerifyConnection()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }

        public void ProcessMigrations()
        {
            /*
id, name, description, assetType, local, temporary, create_time, access_time, asset_flags, CreatorID, data
             */
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "assetdata", Migrations_AssetData, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "assetrefs", Migrations_AssetRefs, m_Log);
        }

        private static readonly string[] Migrations_AssetData = new string[]
        {
            "CREATE TABLE %tablename% (" +
                    "hash CHAR(26) NOT NULL," +
                    "assetType INT(11) NOT NULL," + 
                    "data LONGBLOB," + 
                    "PRIMARY KEY(hash, assetType)" +
                    ") ROW_FORMAT=DYNAMIC"
        };

        private static readonly string[] Migrations_AssetRefs = new string[]
        {
            "CREATE TABLE %tablename% (" +
                    "id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                    "name VARCHAR(128) NOT NULL DEFAULT ''," +
                    "description VARCHAR(255) NOT NULL DEFAULT ''," + 
                    "assetType INT(11) NOT NULL," + 
                    "local INT(1) NOT NULL," + 
                    "temporary INT(1) NOT NULL," + 
                    "create_time BIGINT(20) NOT NULL," +
                    "access_time BIGINT(20) NOT NULL," +
                    "asset_flags INT(11) NOT NULL," +
                    "CreatorID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                    "hash CHAR(26) NOT NULL," + 
                    "PRIMARY KEY(id)" + 
                    ") ROW_FORMAT=DYNAMIC"
        };
        #endregion

        private static readonly int MAX_ASSET_NAME = 128;
        private static readonly int MAX_ASSET_DESC = 255;
    }
    #endregion

    #region Factory
    [PluginName("DedupAssets")]
    public class MySQLDedupAssetServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL DEDUP ASSET SERVICE");
        public MySQLDedupAssetServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLDedupAssetService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
