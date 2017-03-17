// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage : ISimulationDataParcelStorageInterface
    {
        readonly MySQLSimulationDataParcelAccessListStorage m_WhiteListStorage;
        readonly MySQLSimulationDataParcelAccessListStorage m_BlackListStorage;

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        ParcelInfo ISimulationDataParcelStorageInterface.this[UUID regionID, UUID parcelID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM parcels WHERE RegionID LIKE '" + regionID.ToString() + "' AND ParcelID LIKE '" + parcelID.ToString() + "'", connection))
                    {
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

        bool ISimulationDataParcelStorageInterface.Remove(UUID regionID, UUID parcelID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM parcels WHERE RegionID LIKE '" + regionID.ToString() + "' AND ParcelID LIKE '" + parcelID.ToString() + "'", connection))
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        List<UUID> ISimulationDataParcelStorageInterface.ParcelsInRegion(UUID key)
        {
            List<UUID> parcels = new List<UUID>();
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using(MySqlCommand cmd = new MySqlCommand("SELECT ParcelID FROM parcels WHERE RegionID LIKE '" + key.ToString() + "'", connection))
                {
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

        void ISimulationDataParcelStorageInterface.Store(UUID regionID, ParcelInfo parcel)
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

        ISimulationDataParcelAccessListStorageInterface ISimulationDataParcelStorageInterface.WhiteList
        {
            get
            {
                return m_WhiteListStorage;
            }
        }

        ISimulationDataParcelAccessListStorageInterface ISimulationDataParcelStorageInterface.BlackList
        {
            get
            {
                return m_BlackListStorage;
            }
        }
    }
}
