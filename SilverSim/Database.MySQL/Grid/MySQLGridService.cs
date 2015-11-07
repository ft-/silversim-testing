// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Grid
{
    #region Service Implementation
    public sealed class MySQLGridService : GridServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GRID SERVICE");
        private bool DeleteOnUnregister
        {
            get
            {
                return m_ServerParams.GetBoolean(UUID.Zero, "DeleteOnUnregister", false);
            }
        }

        private bool AllowDuplicateRegionNames
        {
            get
            {
                return m_ServerParams.GetBoolean(UUID.Zero, "AllowDuplicateRegionNames", false);
            }
        }

        private ServerParamServiceInterface m_ServerParams;

        #region Constructor
        public MySQLGridService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ServerParams = loader.GetService<ServerParamServiceInterface>("ServerParamStorage");
        }
        #endregion

        public void VerifyConnection()
        {
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "regions", Migrations, m_Log);
        }

        #region Accessors
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, UUID regionID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE uuid LIKE ?id AND ScopeID LIKE ?scopeid", connection))
                    {
                        cmd.Parameters.AddWithValue("?id", regionID.ToString());
                        cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return ToRegionInfo(dbReader);
                            }
                        }
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, uint gridX, uint gridY]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE locX <= ?x AND locY <= ?y AND locX + sizeX > ?x AND locY + sizeY > ?y AND ScopeID LIKE ?scopeid", connection))
                    {
                        cmd.Parameters.AddWithValue("?x", gridX);
                        cmd.Parameters.AddWithValue("?y", gridY);
                        cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return ToRegionInfo(dbReader);
                            }
                        }
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, string regionName]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE regionName LIKE ?name AND ScopeID LIKE ?scopeid", connection))
                    {
                        cmd.Parameters.AddWithValue("?name", regionName);
                        cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return ToRegionInfo(dbReader);
                            }
                        }
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        public override RegionInfo this[UUID regionID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE uuid LIKE ?id", connection))
                    {
                        cmd.Parameters.AddWithValue("?id", regionID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return ToRegionInfo(dbReader);
                            }
                        }
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        #endregion

        #region dbData to RegionInfo
        private RegionInfo ToRegionInfo(MySqlDataReader dbReader)
        {
            RegionInfo ri = new RegionInfo();
            ri.ID = dbReader["uuid"].ToString();
            ri.Name = dbReader["regionName"].ToString();
            ri.RegionSecret = dbReader["regionSecret"].ToString();
            ri.ServerIP = (string)dbReader["serverIP"];
            ri.ServerPort = (uint)dbReader["serverPort"];
            ri.ServerURI = (string)dbReader["serverURI"];
            ri.Location.X = (uint)dbReader["locX"];
            ri.Location.Y = (uint)dbReader["locY"];
            ri.RegionMapTexture = dbReader["regionMapTexture"].ToString();
            ri.ServerHttpPort = (uint)dbReader["serverHttpPort"];
            ri.Owner = new UUI(dbReader["owner"].ToString());
            ri.Access = (RegionAccess)(uint)dbReader["access"];
            ri.ScopeID = dbReader["ScopeID"].ToString();
            ri.Size.X = (uint)dbReader["sizeX"];
            ri.Size.Y = (uint)dbReader["sizeY"];
            ri.Flags = (RegionFlags)(uint)dbReader["flags"];
            ri.AuthenticatingToken = dbReader["AuthenticatingToken"].ToString();
            ri.AuthenticatingPrincipalID = dbReader["AuthenticatingPrincipalID"].ToString();
            ri.ParcelMapTexture = dbReader["parcelMapTexture"].ToString();

            return ri;
        }
        #endregion

        #region Region Registration
        public override void RegisterRegion(RegionInfo regionInfo)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();

                if(!AllowDuplicateRegionNames)
                {
                    using(MySqlCommand cmd = new MySqlCommand("SELECT uuid FROM regions WHERE ScopeID LIKE ?scopeid AND regionName LIKE ?name LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("?scopeid", regionInfo.ScopeID.ToString());
                        cmd.Parameters.AddWithValue("?name", regionInfo.Name);
                        using(MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                if (dbReader["uuid"].ToString() != regionInfo.ID.ToString())
                                {
                                    throw new GridRegionUpdateFailedException("Duplicate region name");
                                }
                            }
                        }
                    }
                }

                /* we have to give checks for all intersection variants */
                using(MySqlCommand cmd = new MySqlCommand("SELECT uuid FROM regions WHERE (" +
                            "(locX >= ?xmin AND locY >= ?ymin AND locX < ?xmax AND locY < ?ymax) OR " +
                            "(locX + sizeX > ?xmin AND locY+sizeY > ?ymin AND locX + sizeX < ?xmax AND locY + sizeY < ?ymax)" +
                            ") AND uuid NOT LIKE ?regionid AND " +
                            "ScopeID LIKE ?scopeid LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("?xmin", regionInfo.Location.X);
                    cmd.Parameters.AddWithValue("?ymin", regionInfo.Location.Y);
                    cmd.Parameters.AddWithValue("?xmax", regionInfo.Location.X + regionInfo.Size.X);
                    cmd.Parameters.AddWithValue("?ymax", regionInfo.Location.Y + regionInfo.Size.Y);
                    cmd.Parameters.AddWithValue("?regionid", regionInfo.ID.ToString());
                    cmd.Parameters.AddWithValue("?scopeid", regionInfo.ScopeID.ToString());
                    using(MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            if ((string)dbReader["uuid"] != regionInfo.ID.ToString())
                            {
                                throw new GridRegionUpdateFailedException("Overlapping regions");
                            }
                        }
                    }
                }

                Dictionary<string, object> regionData = new Dictionary<string, object>();
                regionData["uuid"] = regionInfo.ID.ToString();
                regionData["regionName"] = regionInfo.Name;
                regionData["loc"] = regionInfo.Location;
                regionData["size"] = regionInfo.Size;
                regionData["regionName"] = regionInfo.Name;
                regionData["serverIP"] = regionInfo.ServerIP;
                regionData["serverHttpPort"] = regionInfo.ServerHttpPort;
                regionData["serverURI"] = regionInfo.ServerURI;
                regionData["serverPort"] = regionInfo.ServerPort;
                regionData["regionMapTexture"] = regionInfo.RegionMapTexture;
                regionData["parcelMapTexture"] = regionInfo.ParcelMapTexture;
                regionData["access"] = (uint)regionInfo.Access;
                regionData["regionSecret"] = regionInfo.RegionSecret;
                regionData["owner"] = regionInfo.Owner.ToString();
                regionData["AuthenticatingToken"] = regionInfo.AuthenticatingToken;
                regionData["AuthenticatingPrincipalID"] = regionInfo.AuthenticatingPrincipalID.ToString();
                regionData["flags"] = (uint)regionInfo.Flags;
                regionData["ScopeID"] = regionInfo.ScopeID.ToString();

                MySQLUtilities.ReplaceInsertInto(conn, "regions", regionData);
            }
        }

        public override void UnregisterRegion(UUID scopeID, UUID regionID)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();

                if(DeleteOnUnregister)
                {
                    /* we handoff most stuff to mysql here */
                    /* first line deletes only when region is not persistent */
                    using(MySqlCommand cmd = new MySqlCommand("DELETE FROM regions WHERE ScopeID LIKE ?scopeid AND uuid LIKE ?regionid AND (flags & ?persistent) != 0", conn))
                    {
                        cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                        cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                        cmd.Parameters.AddWithValue("?persistent", (uint)RegionFlags.Persistent);
                        cmd.ExecuteNonQuery();
                    }

                    /* second step is to set it offline when it is persistent */
                }

                using (MySqlCommand cmd = new MySqlCommand("UPDATE regions SET flags = flags - ?online, last_seen=?unixtime WHERE ScopeID LIKE ?scopeid AND uuid LIKE ?regionid AND (flags & ?online) != 0", conn))
                {
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    cmd.Parameters.AddWithValue("?online", (uint)RegionFlags.RegionOnline);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void DeleteRegion(UUID scopeID, UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM regions WHERE ScopeID LIKE ?scopeid AND uuid LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region List accessors
        public override List<RegionInfo> GetDefaultRegions(UUID scopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE flags & ?flag != 0 AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddWithValue("?flag", (uint)RegionFlags.DefaultRegion);
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while(dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetOnlineRegions(UUID scopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE flags & ?flag != 0 AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddWithValue("?flag", (uint)RegionFlags.RegionOnline);
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetOnlineRegions()
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE flags & ?flag != 0", connection))
                {
                    cmd.Parameters.AddWithValue("?flag", (uint)RegionFlags.RegionOnline);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetFallbackRegions(UUID scopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE flags & ?flag != 0 AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddWithValue("?flags", (uint)RegionFlags.FallbackRegion);
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetDefaultHypergridRegions(UUID scopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE flags & ?flag != 0 AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddWithValue("?flags", (uint)RegionFlags.DefaultHGRegion);
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetRegionsByRange(UUID scopeID, GridVector min, GridVector max)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE (" +
                        "(locX >= ?xmin AND locY >= ?ymin AND locX <= ?xmax AND locY <= ?ymax) OR " +
                        "(locX + sizeX >= ?xmin AND locY+sizeY >= ?ymin AND locX + sizeX <= ?xmax AND locY + sizeY <= ?ymax) OR " +
                        "(locX >= ?xmin AND locY >= ?ymin AND locX + sizeX > ?xmin AND locY + sizeY > ?ymin) OR " +
                        "(locX >= ?xmax AND locY >= ?ymax AND locX + sizeX > ?xmax AND locY + sizeY > ?ymax)" +
                        ") AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    cmd.Parameters.AddWithValue("?xmin", min.X);
                    cmd.Parameters.AddWithValue("?ymin", min.Y);
                    cmd.Parameters.AddWithValue("?xmax", max.X);
                    cmd.Parameters.AddWithValue("?ymax", max.Y);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetNeighbours(UUID scopeID, UUID regionID)
        {
            RegionInfo ri = this[scopeID, regionID];
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE (" +
                                                            "((locX = ?maxX OR locX + sizeX = ?locX)  AND "+
                                                            "(locY <= ?maxY AND locY + sizeY >= ?locY))" +
                                                            " OR " +
                                                            "((locY = ?maxY OR locY + sizeY = ?locY) AND " +
                                                            "(locX <= ?maxX AND locX + sizeX >= ?locX))" +
                                                            ") AND " +
                                                            "ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    cmd.Parameters.AddWithValue("?locX", ri.Location.X);
                    cmd.Parameters.AddWithValue("?locY", ri.Location.Y);
                    cmd.Parameters.AddWithValue("?maxX", (ri.Size.X + ri.Location.X));
                    cmd.Parameters.AddWithValue("?maxY", (ri.Size.Y + ri.Location.Y));
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;

        }

        public override List<RegionInfo> GetAllRegions(UUID scopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions", connection))
                {
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> SearchRegionsByName(UUID scopeID, string searchString)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regions WHERE ScopeID LIKE ?scopeid AND regionName LIKE '"+MySqlHelper.EscapeString(searchString)+"%'", connection))
                {
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override Dictionary<string, string> GetGridExtraFeatures()
        {
            return new Dictionary<string, string>();
        }

        #endregion

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "uuid CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "regionName VARCHAR(128)," +
                "regionSecret VARCHAR(128)," +
                "serverIP VARCHAR(64)," +
                "serverPort INT(10) UNSIGNED," +
                "serverURI VARCHAR(255)," +
                "locX INT(10) UNSIGNED," +
                "locY INT(10) UNSIGNED," +
                "regionMapTexture CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "serverHttpPort INT(10) UNSIGNED," +
                "owner VARCHAR(255) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "access INT(10) UNSIGNED DEFAULT '1'," +
                "ScopeID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "sizeX INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "sizeY INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "flags INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "last_seen BIGINT(20) UNSIGNED NOT NULL DEFAULT '0'," +
                "AuthenticatingToken VARCHAR(255) NOT NULL DEFAULT ''," +
                "AuthenticatingPrincipalID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "parcelMapTexture CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "PRIMARY KEY(uuid)," +
                "KEY regionName (regionName)," +
                "KEY ScopeID (ScopeID)," +
                "KEY flags (flags))"
        };
    }
    #endregion

    #region Factory
    [PluginName("Grid")]
    public class MySQLGridServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GRID SERVICE");
        public MySQLGridServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLGridService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion

}
