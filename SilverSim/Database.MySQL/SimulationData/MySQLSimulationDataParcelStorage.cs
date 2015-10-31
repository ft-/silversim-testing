// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataParcelStorage : SimulationDataParcelStorageInterface
    {
        private string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");

        public MySQLSimulationDataParcelStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override ParcelInfo this[UUID regionID, UUID parcelID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM parcels WHERE RegionID LIKE ?regionID AND ParcelID LIKE ?parcelID", connection))
                    {
                        cmd.Parameters.AddWithValue("?regionID", regionID.ToString());
                        cmd.Parameters.AddWithValue("?parcelID", parcelID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (!dbReader.Read())
                            {
                                throw new KeyNotFoundException();
                            }

                            ParcelInfo pi = new ParcelInfo((int)dbReader["BitmapWidth"], (int)dbReader["BitmapHeight"]);
                            pi.Area = (int)(ulong)dbReader["Area"];
                            pi.AuctionID = (uint)dbReader["AuctionID"];
                            pi.AuthBuyer = new UUI((string)dbReader["AuthBuyer"]);
                            pi.Category = (ParcelCategory)(int)dbReader["Category"];
                            pi.ClaimDate = Date.UnixTimeToDateTime((ulong)dbReader["ClaimDate"]);
                            pi.ClaimPrice = (int)dbReader["ClaimPrice"];
                            pi.ID = MySQLUtilities.GetUUID(dbReader, "ParcelID");
                            pi.Group = new UGI((string)dbReader["Group"]);
                            pi.GroupOwned = MySQLUtilities.GetBoolean(dbReader, "IsGroupOwned");
                            pi.Description = (string)dbReader["Description"];
                            pi.Flags = (ParcelFlags)(uint)dbReader["Flags"];
                            pi.LandingType = (TeleportLandingType)(int)dbReader["LandingType"];
                            pi.LandingPosition = MySQLUtilities.GetVector(dbReader, "LandingPosition");
                            pi.LandingLookAt = MySQLUtilities.GetVector(dbReader, "LandingLookAt");
                            pi.Name = (string)dbReader["Name"];
                            pi.LocalID = (int)dbReader["LocalID"];
                            if (!string.IsNullOrEmpty((string)dbReader["MusicURI"]))
                            {
                                pi.MusicURI = new URI((string)dbReader["MusicURI"]);
                            }
                            if (!string.IsNullOrEmpty((string)dbReader["MediaURI"]))
                            {
                                pi.MediaURI = new URI((string)dbReader["MediaURI"]);
                            }
                            pi.MediaID = MySQLUtilities.GetUUID(dbReader, "MediaID");
                            pi.Owner = new UUI((string)dbReader["Owner"]);
                            pi.SnapshotID = MySQLUtilities.GetUUID(dbReader, "SnapshotID");
                            pi.SalePrice = (int)dbReader["SalePrice"];
                            pi.OtherCleanTime = (int)(int)dbReader["OtherCleanTime"];
                            pi.MediaAutoScale = (uint)dbReader["MediaAutoScale"] != 0;
                            pi.RentPrice = (int)dbReader["RentPrice"];
                            pi.AABBMin = MySQLUtilities.GetVector(dbReader, "AABBMin");
                            pi.AABBMax = MySQLUtilities.GetVector(dbReader, "AABBMax");
                            pi.ParcelPrimBonus = (double)dbReader["ParcelPrimBonus"];
                            pi.PassPrice = (int)dbReader["PassPrice"];
                            pi.PassHours = (double)dbReader["PassHours"];
                            pi.ActualArea = (int)(ulong)dbReader["ActualArea"];
                            pi.BillableArea = (int)(ulong)dbReader["BillAbleArea"];
                            pi.LandBitmap.DataNoAABBUpdate = (byte[])dbReader["Bitmap"];
                            pi.Status = (ParcelStatus)(int)dbReader["Status"];
                            return pi;
                        }
                    }
                }
            }
        }

        public override List<UUID> ParcelsInRegion(UUID key)
        {
            List<UUID> parcels = new List<UUID>();
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using(MySqlCommand cmd = new MySqlCommand("SELECT ParcelID FROM parcels WHERE RegionID LIKE ?regionID", connection))
                {
                    cmd.Parameters.AddWithValue("?regionID", key.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            parcels.Add(new UUID((Guid)dbReader["ParcelID"]));
                        }
                    }
                }
            }
            return parcels;
        }

        public override void Store(UUID regionID, ParcelInfo parcel)
        {
            Dictionary<string, object> p = new Dictionary<string, object>();
            p["RegionID"] = regionID.ToString();
            p["ParcelID"] = parcel.ID.ToString();
            p["LocalID"] = parcel.LocalID;
            p["Bitmap"] = parcel.LandBitmap.Data;
            p["BitmapWidth"] = parcel.LandBitmap.BitmapWidth;
            p["BitmapHeight"] = parcel.LandBitmap.BitmapHeight;
            p["Name"] = parcel.Name;
            p["Description"] = parcel.Description;
            p["Owner"] = parcel.Owner.ToString();
            p["IsGroupOwned"] = parcel.GroupOwned ? 1 : 0;
            p["Area"] = parcel.Area;
            p["AuctionID"] = parcel.AuctionID.ToString();
            p["AuthBuyer"] = parcel.AuthBuyer.ToString();
            p["Category"] = (int)parcel.Category;
            p["ClaimDate"] = parcel.ClaimDate.AsULong;
            p["ClaimPrice"] = parcel.ClaimPrice;
            p["Group"] = parcel.Group.ToString();
            p["Flags"] = (uint)parcel.Flags;
            p["LandingType"] = (int)parcel.LandingType;
            p["LandingPosition"] = parcel.LandingPosition;
            p["LandingLookAt"] = parcel.LandingLookAt;
            p["Status"] = (int)parcel.Status;
            p["MusicURI"] = parcel.MusicURI;
            p["MediaURI"] = parcel.MediaURI;
            p["MediaID"] = parcel.MediaID.ToString();
            p["SnapshotID"] = parcel.SnapshotID.ToString();
            p["SalePrice"] = parcel.SalePrice;
            p["OtherCleanTime"] = parcel.OtherCleanTime;
            p["MediaAutoScale"] = parcel.MediaAutoScale ? 1 : 0;
            p["RentPrice"] = parcel.RentPrice;
            p["AABBMin"] = parcel.AABBMin;
            p["AABBMax"] = parcel.AABBMax;
            p["ParcelPrimBonus"] = parcel.ParcelPrimBonus;
            p["PassPrice"] = parcel.PassPrice;
            p["PassHours"] = parcel.PassHours;
            p["ActualArea"] = parcel.ActualArea;
            p["BillableArea"] = parcel.BillableArea;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                MySQLUtilities.ReplaceInsertInto(connection, "parcels", p);
            }
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        #region Migrations
        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "parcels", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ParcelID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "LocalID int(11) NOT NULL DEFAULT '0'," +
                "Bitmap LONGBLOB," +
                "BitmapWidth INT(11) NOT NULL DEFAULT '0'," +
                "BitmapHeight INT(11) NOT NULL DEFAULT '0'," +
                "`Name` VARCHAR(255) NOT NULL DEFAULT ''," +
                "Description TEXT," +
                "`Owner` VARCHAR(255) NOT NULL DEFAULT ''," +
                "IsGroupOwned TINYINT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "Area BIGINT(20) UNSIGNED NOT NULL," +
                "AuctionID INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "AuthBuyer VARCHAR(255) NOT NULL DEFAULT ''," +
                "Category INT(11) NOT NULL DEFAULT '0'," +
                "ClaimDate BIGINT(20) UNSIGNED NOT NULL DEFAULT '0'," +
                "ClaimPrice INT(11) NOT NULL DEFAULT '0'," +
                "`Group` VARCHAR(255) NOT NULL DEFAULT ''," +
                "Flags INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "LandingType INT(11) NOT NULL DEFAULT '0'," +
                "LandingPositionX DOUBLE NOT NULL DEFAULT '0'," +
                "LandingPositionY DOUBLE NOT NULL DEFAULT '0'," +
                "LandingPositionZ DOUBLE NOT NULL DEFAULT '0'," +
                "LandingLookAtX DOUBLE NOT NULL DEFAULT '0'," +
                "LandingLookAtY DOUBLE NOT NULL DEFAULT '0'," +
                "LandingLookAtZ DOUBLE NOT NULL DEFAULT '0'," +
                "`Status` INT(11) NOT NULL DEFAULT '0'," +
                "MusicURI VARCHAR(255) NOT NULL DEFAULT ''," +
                "MediaURI VARCHAR(255) NOT NULL DEFAULT ''," +
                "MediaID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "SnapshotID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "SalePrice INT(11) NOT NULL DEFAULT '0'," +
                "OtherCleanTime INT(11) NOT NULL DEFAULT '0'," +
                "MediaAutoScale INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "RentPrice INT(11) NOT NULL DEFAULT '0'," +
                "AABBMinX DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMinY DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMinZ DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMaxX DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMaxY DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMaxZ DOUBLE NOT NULL DEFAULT '0'," +
                "ParcelPrimBonus DOUBLE NOT NULL DEFAULT '1'," +
                "PassPrice INT(11) NOT NULL DEFAULT '0'," +
                "PassHours DOUBLE NOT NULL DEFAULT '0'," +
                "ActualArea BIGINT(20) UNSIGNED NOT NULL DEFAULT '0'," +
                "BillableArea BIGINT(20) UNSIGNED NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(RegionID, ParcelID)," +
                "KEY ParcelNames (RegionID, `Name`)," +
                "UNIQUE KEY LocalIDs (RegionID, LocalID))"
        };
        #endregion
    }
}
