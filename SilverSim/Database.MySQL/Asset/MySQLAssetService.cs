// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Asset
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class MySQLAssetService : AssetServiceInterface, IDBServiceInterface, IPlugin
    {
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL ASSET SERVICE");

        readonly string m_ConnectionString;
        readonly MySQLAssetMetadataService m_MetadataService;
        readonly DefaultAssetReferencesService m_ReferencesService;
        readonly MySQLAssetDataService m_DataService;

        #region Constructor
        public MySQLAssetService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_MetadataService = new MySQLAssetMetadataService(connectionString);
            m_DataService = new MySQLAssetDataService(connectionString);
            m_ReferencesService = new DefaultAssetReferencesService(this);
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        #region Exists methods
        public override bool Exists(UUID key)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT id, access_time FROM assets WHERE id LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", key.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if(dbReader.Read())
                        {
                            if (dbReader.GetDate("access_time") - DateTime.UtcNow > TimeSpan.FromHours(1))
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
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override Dictionary<UUID, bool> Exists(List<UUID> assets)
        {
            Dictionary<UUID,bool> res = new Dictionary<UUID,bool>();
            if (assets.Count == 0)
                return res;

            foreach(UUID id in assets)
            {
                res[id] = false;
            }

            string ids = "'" + string.Join("','", assets) + "'";
            string sql = string.Format("SELECT id, access_time FROM assets WHERE id IN ({0})", ids);

            using (MySqlConnection dbcon = new MySqlConnection(m_ConnectionString))
            {
                dbcon.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, dbcon))
                {
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            UUID id = dbReader.GetUUID("id");
                            res[id] = true;
                            if (dbReader.GetDate("access_time") - DateTime.UtcNow > TimeSpan.FromHours(1))
                            {
                                /* update access_time */
                                using (MySqlConnection uconn = new MySqlConnection(m_ConnectionString))
                                {
                                    uconn.Open();
                                    using (MySqlCommand ucmd = new MySqlCommand("UPDATE assets SET access_time = ?access WHERE id LIKE ?id", uconn))
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM assets WHERE id LIKE ?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", key.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                AssetData asset = new AssetData();
                                asset.ID = dbReader.GetUUID("id");
                                asset.Data = dbReader.GetBytes("data");
                                asset.Type = (AssetType)(int)dbReader["assetType"];
                                asset.Name = (string)dbReader["name"];
                                asset.CreateTime = dbReader.GetDate("create_time");
                                asset.AccessTime = dbReader.GetDate("access_time");
                                asset.Creator.ID = dbReader.GetUUID("CreatorID");
                                asset.Flags = dbReader.GetAssetFlags("asset_flags");
                                asset.Temporary = dbReader.GetBoolean("temporary");

                                if (asset.CreateTime - DateTime.UtcNow > TimeSpan.FromHours(1))
                                {
                                    /* update access_time */
                                    using (MySqlConnection uconn = new MySqlConnection(m_ConnectionString))
                                    {
                                        uconn.Open();
                                        using (MySqlCommand ucmd = new MySqlCommand("UPDATE assets SET access_time = ?access WHERE id LIKE ?id", uconn))
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
                throw new AssetNotFoundException(key);
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
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override void Store(AssetData asset)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();

                using (MySqlCommand cmd =
                    new MySqlCommand(
                        "INSERT INTO assets(id, name, assetType, temporary, create_time, access_time, asset_flags, CreatorID, data)" +
                        "VALUES(?id, ?name, ?assetType, ?temporary, ?create_time, ?access_time, ?asset_flags, ?CreatorID, ?data)",
                        conn))
                {
                    string assetName = asset.Name;
                    if (asset.Name.Length > MAX_ASSET_NAME)
                    {
                        assetName = asset.Name.Substring(0, MAX_ASSET_NAME);
                        m_Log.WarnFormat("Name '{0}' for asset {1} truncated from {2} to {3} characters on add",
                            asset.Name, asset.ID, asset.Name.Length, assetName.Length);
                    }

                    try
                    {
                        using (cmd)
                        {
                            // create unix epoch time
                            ulong now = Date.GetUnixTime();
                            cmd.Parameters.AddWithValue("?id", asset.ID.ToString());
                            cmd.Parameters.AddWithValue("?name", assetName);
                            cmd.Parameters.AddWithValue("?assetType", (int)asset.Type);
                            cmd.Parameters.AddWithValue("?temporary", asset.Temporary);
                            cmd.Parameters.AddWithValue("?create_time", now);
                            cmd.Parameters.AddWithValue("?access_time", now);
                            cmd.Parameters.AddWithValue("?CreatorID", asset.Creator.ID.ToString());
                            cmd.Parameters.AddWithValue("?asset_flags", (uint)asset.Flags);
                            cmd.Parameters.AddWithValue("?data", asset.Data);
                            if(1 > cmd.ExecuteNonQuery())
                            {
                                throw new AssetStoreFailedException(asset.ID);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        m_Log.Error(
                            string.Format("MySQL failure creating asset {0} with name {1}.  Exception  ",
                                asset.ID, asset.Name)
                            , e);
                        throw new AssetStoreFailedException(asset.ID);
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM assets WHERE id=?id AND asset_flags <> 0", conn))
                {
                    cmd.Parameters.AddWithValue("?id", id.ToString());
                    if(cmd.ExecuteNonQuery() < 1)
                    {
                        throw new AssetNotDeletedException(id);
                    }
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "assets", Migrations_Assets, m_Log);
        }

        private static readonly string[] Migrations_Assets = new string[]
        {
            "CREATE TABLE %tablename% (" +
                    "id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                    "name VARCHAR(64) NOT NULL DEFAULT ''," +
                    "assetType INT(11) NOT NULL," + 
                    "temporary INT(1) NOT NULL," + 
                    "create_time BIGINT(20) NOT NULL," +
                    "access_time BIGINT(20) NOT NULL," +
                    "asset_flags INT(11) NOT NULL," +
                    "CreatorID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                    "data LONGBLOB," + 
                    "PRIMARY KEY(id)" + 
                    ") ROW_FORMAT=DYNAMIC"
        };
        #endregion

        private const int MAX_ASSET_NAME = 64;
    }
    #endregion

    #region Factory
    [PluginName("Assets")]
    public class MySQLAssetServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ASSET SERVICE");
        public MySQLAssetServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLAssetService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
