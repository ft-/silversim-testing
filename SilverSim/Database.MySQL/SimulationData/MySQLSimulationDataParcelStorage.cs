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
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");
        readonly MySQLSimulationDataParcelAccessListStorage m_WhiteListStorage;
        readonly MySQLSimulationDataParcelAccessListStorage m_BlackListStorage;

        public MySQLSimulationDataParcelStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_WhiteListStorage = new MySQLSimulationDataParcelAccessListStorage(connectionString, "parcelaccesswhitelist");
            m_BlackListStorage = new MySQLSimulationDataParcelAccessListStorage(connectionString, "parcelaccessblacklist");
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
                        cmd.Parameters.AddParameter("?regionID", regionID);
                        cmd.Parameters.AddParameter("?parcelID", parcelID);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (!dbReader.Read())
                            {
                                throw new KeyNotFoundException();
                            }

                            ParcelInfo pi = new ParcelInfo((int)dbReader["BitmapWidth"], (int)dbReader["BitmapHeight"]);
                            pi.Area = dbReader.GetInt32("Area");
                            pi.AuctionID = dbReader.GetUInt32("AuctionID");
                            pi.AuthBuyer = dbReader.GetUUI("AuthBuyer");
                            pi.Category = dbReader.GetEnum<ParcelCategory>("Category");
                            pi.ClaimDate = dbReader.GetDate("ClaimDate");
                            pi.ClaimPrice = dbReader.GetInt32("ClaimPrice");
                            pi.ID = dbReader.GetUUID("ParcelID");
                            pi.Group = dbReader.GetUGI("Group");
                            pi.GroupOwned = dbReader.GetBool("IsGroupOwned");
                            pi.Description = dbReader.GetString("Description");
                            pi.Flags = dbReader.GetEnum<ParcelFlags>("Flags");
                            pi.LandingType = dbReader.GetEnum<TeleportLandingType>("LandingType");
                            pi.LandingPosition = dbReader.GetVector3("LandingPosition");
                            pi.LandingLookAt = dbReader.GetVector3("LandingLookAt");
                            pi.Name = dbReader.GetString("Name");
                            pi.LocalID = dbReader.GetInt32("LocalID");
                            string uri = (string)dbReader["MusicURI"];
                            if (!string.IsNullOrEmpty(uri))
                            {
                                pi.MusicURI = new URI(uri);
                            }
                            uri = (string)dbReader["MediaURI"];
                            if (!string.IsNullOrEmpty(uri))
                            {
                                pi.MediaURI = new URI(uri);
                            }
                            pi.MediaID = dbReader.GetUUID("MediaID");
                            pi.Owner = dbReader.GetUUI("Owner");
                            pi.SnapshotID = dbReader.GetUUID("SnapshotID");
                            pi.SalePrice = dbReader.GetInt32("SalePrice");
                            pi.OtherCleanTime = dbReader.GetInt32("OtherCleanTime");
                            pi.MediaAutoScale = dbReader.GetBool("MediaAutoScale");
                            pi.MediaType = dbReader.GetString("MediaType");
                            pi.MediaWidth = dbReader.GetInt32("MediaWidth");
                            pi.MediaHeight = dbReader.GetInt32("MediaHeight");
                            pi.MediaLoop = dbReader.GetBool("MediaLoop");
                            pi.ObscureMedia = dbReader.GetBool("ObscureMedia");
                            pi.ObscureMusic = dbReader.GetBool("ObscureMusic");
                            pi.MediaDescription = dbReader.GetString("MediaDescription");
                            pi.RentPrice = dbReader.GetInt32("RentPrice");
                            pi.AABBMin = dbReader.GetVector3("AABBMin");
                            pi.AABBMax = dbReader.GetVector3("AABBMax");
                            pi.ParcelPrimBonus = dbReader.GetDouble("ParcelPrimBonus");
                            pi.PassPrice = dbReader.GetInt32("PassPrice");
                            pi.PassHours = dbReader.GetDouble("PassHours");
                            pi.ActualArea = dbReader.GetInt32("ActualArea");
                            pi.BillableArea = dbReader.GetInt32("BillAbleArea");
                            pi.LandBitmap.DataNoAABBUpdate = dbReader.GetBytes("Bitmap");
                            pi.Status = dbReader.GetEnum<ParcelStatus>("Status");
                            pi.SeeAvatars = dbReader.GetBool("SeeAvatars");
                            pi.AnyAvatarSounds = dbReader.GetBool("AnyAvatarSounds");
                            pi.GroupAvatarSounds = dbReader.GetBool("GroupAvatarSounds");
                            pi.IsPrivate = dbReader.GetBool("IsPrivate");
                            return pi;
                        }
                    }
                }
            }
        }

        public override bool Remove(UUID regionID, UUID parcelID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM parcels WHERE RegionID LIKE ?regionID AND ParcelID LIKE ?parcelID", connection))
                {
                    cmd.Parameters.AddParameter("?regionID", regionID);
                    cmd.Parameters.AddParameter("?parcelID", parcelID);
                    return cmd.ExecuteNonQuery() > 0;
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
                    cmd.Parameters.AddParameter("?regionID", key);
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
            p["RegionID"] = regionID;
            p["ParcelID"] = parcel.ID;
            p["LocalID"] = parcel.LocalID;
            p["Bitmap"] = parcel.LandBitmap.Data;
            p["BitmapWidth"] = parcel.LandBitmap.BitmapWidth;
            p["BitmapHeight"] = parcel.LandBitmap.BitmapHeight;
            p["Name"] = parcel.Name;
            p["Description"] = parcel.Description;
            p["Owner"] = parcel.Owner;
            p["IsGroupOwned"] = parcel.GroupOwned;
            p["Area"] = parcel.Area;
            p["AuctionID"] = parcel.AuctionID;
            p["AuthBuyer"] = parcel.AuthBuyer;
            p["Category"] = parcel.Category;
            p["ClaimDate"] = parcel.ClaimDate.AsULong;
            p["ClaimPrice"] = parcel.ClaimPrice;
            p["Group"] = parcel.Group;
            p["Flags"] = parcel.Flags;
            p["LandingType"] = parcel.LandingType;
            p["LandingPosition"] = parcel.LandingPosition;
            p["LandingLookAt"] = parcel.LandingLookAt;
            p["Status"] = parcel.Status;
            p["MusicURI"] = parcel.MusicURI;
            p["MediaURI"] = parcel.MediaURI;
            p["MediaType"] = parcel.MediaType;
            p["MediaWidth"] = parcel.MediaWidth;
            p["MediaHeight"] = parcel.MediaHeight;
            p["MediaID"] = parcel.MediaID;
            p["SnapshotID"] = parcel.SnapshotID;
            p["SalePrice"] = parcel.SalePrice;
            p["OtherCleanTime"] = parcel.OtherCleanTime;
            p["MediaAutoScale"] = parcel.MediaAutoScale;
            p["MediaDescription"] = parcel.MediaDescription;
            p["MediaLoop"] = parcel.MediaLoop;
            p["ObscureMedia"] = parcel.ObscureMedia;
            p["ObscureMusic"] = parcel.ObscureMusic;
            p["RentPrice"] = parcel.RentPrice;
            p["AABBMin"] = parcel.AABBMin;
            p["AABBMax"] = parcel.AABBMax;
            p["ParcelPrimBonus"] = parcel.ParcelPrimBonus;
            p["PassPrice"] = parcel.PassPrice;
            p["PassHours"] = parcel.PassHours;
            p["ActualArea"] = parcel.ActualArea;
            p["BillableArea"] = parcel.BillableArea;
            p["SeeAvatars"] = parcel.SeeAvatars;
            p["AnyAvatarSounds"] = parcel.AnyAvatarSounds;
            p["GroupAvatarSounds"] = parcel.GroupAvatarSounds;
            p["IsPrivate"] = parcel.IsPrivate;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                MySQLUtilities.ReplaceInto(connection, "parcels", p);
            }
        }

        public override SimulationDataParcelAccessListStorageInterface WhiteList
        {
            get
            {
                return m_WhiteListStorage;
            }
        }

        public override SimulationDataParcelAccessListStorageInterface BlackList
        {
            get
            {
                return m_BlackListStorage;
            }
        }
    }
}
