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
                        cmd.Parameters.AddWithValue("?regionID", regionID.ToString());
                        cmd.Parameters.AddWithValue("?parcelID", parcelID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (!dbReader.Read())
                            {
                                throw new KeyNotFoundException();
                            }

                            ParcelInfo pi = new ParcelInfo((int)dbReader["BitmapWidth"], (int)dbReader["BitmapHeight"]);
                            pi.Area = (int)dbReader.GetUInt32("Area");
                            pi.AuctionID = (uint)dbReader["AuctionID"];
                            pi.AuthBuyer = dbReader.GetUUI("AuthBuyer");
                            pi.Category = (ParcelCategory)(uint)dbReader["Category"];
                            pi.ClaimDate = dbReader.GetDate("ClaimDate");
                            pi.ClaimPrice = (int)dbReader["ClaimPrice"];
                            pi.ID = dbReader.GetUUID("ParcelID");
                            pi.Group = dbReader.GetUGI("Group");
                            pi.GroupOwned = MySQLUtilities.GetBool(dbReader, "IsGroupOwned");
                            pi.Description = (string)dbReader["Description"];
                            pi.Flags = (ParcelFlags)(uint)dbReader["Flags"];
                            pi.LandingType = (TeleportLandingType)(uint)dbReader["LandingType"];
                            pi.LandingPosition = MySQLUtilities.GetVector3(dbReader, "LandingPosition");
                            pi.LandingLookAt = MySQLUtilities.GetVector3(dbReader, "LandingLookAt");
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
                            pi.MediaID = dbReader.GetUUID("MediaID");
                            pi.Owner = dbReader.GetUUI("Owner");
                            pi.SnapshotID = dbReader.GetUUID("SnapshotID");
                            pi.SalePrice = (int)dbReader["SalePrice"];
                            pi.OtherCleanTime = (int)dbReader["OtherCleanTime"];
                            pi.MediaAutoScale = dbReader.GetBool("MediaAutoScale");
                            pi.MediaType = (string)dbReader["MediaType"];
                            pi.MediaWidth = (int)dbReader["MediaWidth"];
                            pi.MediaHeight = (int)dbReader["MediaHeight"];
                            pi.MediaLoop = dbReader.GetBool("MediaLoop");
                            pi.ObscureMedia = dbReader.GetBool("ObscureMedia");
                            pi.ObscureMusic = dbReader.GetBool("ObscureMusic");
                            pi.MediaDescription = (string)dbReader["MediaDescription"];
                            pi.RentPrice = (int)dbReader["RentPrice"];
                            pi.AABBMin = dbReader.GetVector3("AABBMin");
                            pi.AABBMax = dbReader.GetVector3("AABBMax");
                            pi.ParcelPrimBonus = (double)dbReader["ParcelPrimBonus"];
                            pi.PassPrice = (int)dbReader["PassPrice"];
                            pi.PassHours = (double)dbReader["PassHours"];
                            pi.ActualArea = (int)(ulong)dbReader["ActualArea"];
                            pi.BillableArea = (int)(ulong)dbReader["BillAbleArea"];
                            pi.LandBitmap.DataNoAABBUpdate = (byte[])dbReader["Bitmap"];
                            pi.Status = (ParcelStatus)(uint)dbReader["Status"];
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
                    cmd.Parameters.AddWithValue("?regionID", regionID.ToString());
                    cmd.Parameters.AddWithValue("?parcelID", parcelID.ToString());
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
            p["Owner"] = parcel.Owner;
            p["IsGroupOwned"] = parcel.GroupOwned;
            p["Area"] = parcel.Area;
            p["AuctionID"] = parcel.AuctionID.ToString();
            p["AuthBuyer"] = parcel.AuthBuyer.ToString();
            p["Category"] = parcel.Category;
            p["ClaimDate"] = parcel.ClaimDate.AsULong;
            p["ClaimPrice"] = parcel.ClaimPrice;
            p["Group"] = parcel.Group.ToString();
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
