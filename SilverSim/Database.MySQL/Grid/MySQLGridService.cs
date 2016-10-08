// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Grid
{
    #region Service Implementation
    [Description("MySQL Grid Backend")]
    [ServerParam("DeleteOnUnregister", Type = ServerParamType.GlobalOnly, ParameterType = typeof(bool))]
    [ServerParam("AllowDuplicateRegionNames", Type = ServerParamType.GlobalOnly, ParameterType = typeof(bool))]
    public sealed class MySQLGridService : GridServiceInterface, IDBServiceInterface, IPlugin, IServerParamListener
    {
        readonly string m_ConnectionString;
        readonly string m_TableName;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GRID SERVICE");
        private bool IsDeleteOnUnregister;
        private bool AllowDuplicateRegionNames;

        [ServerParam("DeleteOnUnregister")]
        public void DeleteOnUnregisterUpdated(UUID regionid, string value)
        {
            if(regionid == UUID.Zero)
            {
                IsDeleteOnUnregister = bool.Parse(value);
            }
            
        }

        [ServerParam("AllowDuplicateRegionNames")]
        public void AllowDuplicateRegionNamesUpdated(UUID regionid, string value)
        {
            if (regionid == UUID.Zero)
            {
                AllowDuplicateRegionNames = bool.Parse(value);
            }

        }

        #region Constructor
        public MySQLGridService(string connectionString, string tableName)
        {
            m_ConnectionString = connectionString;
            m_TableName = tableName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
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
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                List<IMigrationElement> migrations = new List<IMigrationElement>();
                migrations.Add(new SqlTable(m_TableName));
                migrations.AddRange(Migrations);
                connection.MigrateTables(migrations.ToArray(), m_Log);
            }
        }

        #region Accessors
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, UUID regionID]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(scopeID, regionID, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID scopeID, UUID regionID, out RegionInfo rInfo)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE uuid LIKE ?id AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddParameter("?id", regionID);
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            rInfo = ToRegionInfo(dbReader);
                            return true;
                        }
                    }
                }
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(UUID scopeID, UUID regionID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE uuid LIKE ?id AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddParameter("?id", regionID);
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, uint gridX, uint gridY]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(scopeID, gridX, gridY, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID scopeID, uint gridX, uint gridY, out RegionInfo rInfo)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE locX <= ?x AND locY <= ?y AND locX + sizeX > ?x AND locY + sizeY > ?y AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddParameter("?x", gridX);
                    cmd.Parameters.AddParameter("?y", gridY);
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            rInfo = ToRegionInfo(dbReader);
                            return true;
                        }
                    }
                }
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(UUID scopeID, uint gridX, uint gridY)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE locX <= ?x AND locY <= ?y AND locX + sizeX > ?x AND locY + sizeY > ?y AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddParameter("?x", gridX);
                    cmd.Parameters.AddParameter("?y", gridY);
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, string regionName]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(scopeID, regionName, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID scopeID, string regionName, out RegionInfo rInfo)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE regionName LIKE ?name AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddParameter("?name", regionName);
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            rInfo = ToRegionInfo(dbReader);
                            return true;
                        }
                    }
                }
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(UUID scopeID, string regionName)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE regionName LIKE ?name AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddParameter("?name", regionName);
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        public override RegionInfo this[UUID regionID]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(regionID, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID regionID, out RegionInfo rInfo)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE uuid LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", regionID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            rInfo = ToRegionInfo(dbReader);
                            return true;
                        }
                    }
                }
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(UUID regionID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE uuid LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", regionID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }
        #endregion

        #region dbData to RegionInfo
        private RegionInfo ToRegionInfo(MySqlDataReader dbReader)
        {
            RegionInfo ri = new RegionInfo();
            ri.ID = dbReader.GetUUID("uuid");
            ri.Name = dbReader.GetString("regionName");
            ri.RegionSecret = dbReader.GetString("regionSecret");
            ri.ServerIP = dbReader.GetString("serverIP");
            ri.ServerPort = dbReader.GetUInt32("serverPort");
            ri.ServerURI = dbReader.GetString("serverURI");
            ri.Location = dbReader.GetGridVector("loc");
            ri.RegionMapTexture = dbReader.GetUUID("regionMapTexture");
            ri.ServerHttpPort = dbReader.GetUInt32("serverHttpPort");
            ri.Owner = dbReader.GetUUI("owner");
            ri.Access = dbReader.GetEnum<RegionAccess>("access");
            ri.ScopeID = dbReader.GetString("ScopeID");
            ri.Size = dbReader.GetGridVector("size");
            ri.Flags = dbReader.GetEnum<RegionFlags>("flags");
            ri.AuthenticatingToken = dbReader.GetString("AuthenticatingToken");
            ri.AuthenticatingPrincipal = dbReader.GetUUI("AuthenticatingPrincipalID");
            ri.ParcelMapTexture = dbReader.GetUUID("parcelMapTexture");
            ri.ProductName = dbReader.GetString("ProductName");

            return ri;
        }
        #endregion

        #region Region Registration
        public override void AddRegionFlags(UUID regionID, RegionFlags setflags)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE `" + MySqlHelper.EscapeString(m_TableName) + "` SET flags = flags | ?flags WHERE uuid LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    cmd.Parameters.AddParameter("?flags", setflags);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void RemoveRegionFlags(UUID regionID, RegionFlags removeflags)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE `" + MySqlHelper.EscapeString(m_TableName) + "` SET flags = flags & ~?flags WHERE uuid LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    cmd.Parameters.AddParameter("?flags", removeflags);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void RegisterRegion(RegionInfo regionInfo)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();

                if(!AllowDuplicateRegionNames)
                {
                    using(MySqlCommand cmd = new MySqlCommand("SELECT uuid FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE ScopeID LIKE ?scopeid AND regionName LIKE ?name LIMIT 1", conn))
                    {
                        cmd.Parameters.AddParameter("?scopeid", regionInfo.ScopeID);
                        cmd.Parameters.AddParameter("?name", regionInfo.Name);
                        using(MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read() && 
                                dbReader.GetUUID("uuid") != regionInfo.ID)
                            {
                                throw new GridRegionUpdateFailedException("Duplicate region name");
                            }
                        }
                    }
                }

                /* we have to give checks for all intersection variants */
                using(MySqlCommand cmd = new MySqlCommand("SELECT uuid FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE (" +
                            "(locX >= ?minx AND locY >= ?miny AND locX < ?maxx AND locY < ?maxy) OR " +
                            "(locX + sizeX > ?minx AND locY+sizeY > ?miny AND locX + sizeX < ?maxx AND locY + sizeY < ?maxy)" +
                            ") AND uuid NOT LIKE ?regionid AND " +
                            "ScopeID LIKE ?scopeid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("?min", regionInfo.Location);
                    cmd.Parameters.AddParameter("?max", (regionInfo.Location + regionInfo.Size));
                    cmd.Parameters.AddParameter("?regionid", regionInfo.ID);
                    cmd.Parameters.AddParameter("?scopeid", regionInfo.ScopeID);
                    using(MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read() && 
                            dbReader.GetUUID("uuid") != regionInfo.ID)
                        {
                            throw new GridRegionUpdateFailedException("Overlapping regions");
                        }
                    }
                }

                Dictionary<string, object> regionData = new Dictionary<string, object>();
                regionData["uuid"] = regionInfo.ID;
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
                regionData["access"] = regionInfo.Access;
                regionData["regionSecret"] = regionInfo.RegionSecret;
                regionData["owner"] = regionInfo.Owner;
                regionData["AuthenticatingToken"] = regionInfo.AuthenticatingToken;
                regionData["AuthenticatingPrincipalID"] = regionInfo.AuthenticatingPrincipal;
                regionData["flags"] = regionInfo.Flags;
                regionData["ScopeID"] = regionInfo.ScopeID;
                regionData["ProductName"] = regionInfo.ProductName;

                MySQLUtilities.ReplaceInto(conn, m_TableName, regionData);
            }
        }

        public override void UnregisterRegion(UUID scopeID, UUID regionID)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();

                if(IsDeleteOnUnregister)
                {
                    /* we handoff most stuff to mysql here */
                    /* first line deletes only when region is not persistent */
                    using(MySqlCommand cmd = new MySqlCommand("DELETE FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE ScopeID LIKE ?scopeid AND uuid LIKE ?regionid AND (flags & ?persistent) != 0", conn))
                    {
                        cmd.Parameters.AddParameter("?scopeid", scopeID);
                        cmd.Parameters.AddParameter("?regionid", regionID);
                        cmd.Parameters.AddParameter("?persistent", RegionFlags.Persistent);
                        cmd.ExecuteNonQuery();
                    }

                    /* second step is to set it offline when it is persistent */
                }

                using (MySqlCommand cmd = new MySqlCommand("UPDATE `" + MySqlHelper.EscapeString(m_TableName) + "` SET flags = flags - ?online, last_seen=?unixtime WHERE ScopeID LIKE ?scopeid AND uuid LIKE ?regionid AND (flags & ?online) != 0", conn))
                {
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    cmd.Parameters.AddParameter("?online", RegionFlags.RegionOnline);
                    cmd.Parameters.AddParameter("?unixtime", Date.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void DeleteRegion(UUID scopeID, UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE ScopeID LIKE ?scopeid AND uuid LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region List accessors
        List<RegionInfo> GetRegionsByFlag(UUID scopeID, RegionFlags flags)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand(scopeID == UUID.Zero ?
                    "SELECT * FROM regions WHERE flags & ?flag != 0" :
                    "SELECT * FROM regions WHERE flags & ?flag != 0 AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddParameter("?flag", flags);
                    if (scopeID != UUID.Zero)
                    {
                        cmd.Parameters.AddParameter("?scopeid", scopeID);
                    }
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

        public override List<RegionInfo> GetHyperlinks(UUID scopeID)
        {
            return GetRegionsByFlag(scopeID, RegionFlags.Hyperlink);
        }

        public override List<RegionInfo> GetDefaultRegions(UUID scopeID)
        {
            return GetRegionsByFlag(scopeID, RegionFlags.DefaultRegion);
        }

        public override List<RegionInfo> GetOnlineRegions(UUID scopeID)
        {
            return GetRegionsByFlag(scopeID, RegionFlags.RegionOnline);
        }

        public override List<RegionInfo> GetOnlineRegions()
        {
            return GetRegionsByFlag(UUID.Zero, RegionFlags.RegionOnline);
        }

        public override List<RegionInfo> GetFallbackRegions(UUID scopeID)
        {
            return GetRegionsByFlag(scopeID, RegionFlags.FallbackRegion);
        }

        public override List<RegionInfo> GetDefaultHypergridRegions(UUID scopeID)
        {
            return GetRegionsByFlag(scopeID, RegionFlags.DefaultHGRegion);
        }

        public override List<RegionInfo> GetRegionsByRange(UUID scopeID, GridVector min, GridVector max)
        {
            List<RegionInfo> result = new List<RegionInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE (" +
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
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE (" +
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
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "`", connection))
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

                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM `" + MySqlHelper.EscapeString(m_TableName) + "` WHERE ScopeID LIKE ?scopeid AND regionName LIKE '"+MySqlHelper.EscapeString(searchString)+"%'", connection))
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

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            /* no SqlTable here since we are adding it when processing migrations */
            new AddColumn<UUID>("uuid") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("regionName") { Cardinality = 128, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("regionSecret") { Cardinality = 128, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("serverIP") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<uint>("serverPort") { IsNullAllowed = false },
            new AddColumn<string>("serverURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<GridVector>("loc") { IsNullAllowed = false, Default = GridVector.Zero },
            new AddColumn<UUID>("regionMapTexture") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("serverHttpPort") { IsNullAllowed = false },
            new AddColumn<UUI>("owner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("access") { IsNullAllowed = false, Default = (uint)13 },
            new AddColumn<UUID>("ScopeID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<GridVector>("Size") { IsNullAllowed = false, Default = GridVector.Zero },
            new AddColumn<uint>("flags") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<Date>("last_seen") { IsNullAllowed = false , Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<string>("AuthenticatingToken") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<UUI>("AuthenticatingPrincipalID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("parcelMapTexture") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo("uuid"),
            new NamedKeyInfo("regionName", "regionName"),
            new NamedKeyInfo("ScopeID", "ScopeID"),
            new NamedKeyInfo("flags", "flags"),
            new TableRevision(2),
            new AddColumn<string>("ProductName") { Cardinality = 255, IsNullAllowed = false, Default = "Mainland" },
            new TableRevision(3),
            /* only used as alter table when revision 2 table exists */
            new ChangeColumn<UUI>("AuthenticatingPrincipalID") { IsNullAllowed = false, Default = UUID.Zero },
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
            return new MySQLGridService(MySQLUtilities.BuildConnectionString(ownSection, m_Log),
                ownSection.GetString("TableName", "regions"));
        }
    }
    #endregion

}
