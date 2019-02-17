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

using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Parcel
{
    [EventQueueGet("ParcelProperties")]
    [Trusted]
    public class ParcelProperties : Message
    {
        public enum RequestResultType : uint
        {
            Single = 0,
            Multiple = 1
        }

        public const int SEQID_HOVERED_OVER_PARCEL = -50000;
        public const int SEQID_NOT_ON_ACCESS_LIST = -40000;
        public const int SEQID_BANNED = -30000;
        public const int SEQID_NOT_IN_GROUP = -20000;
        public const int SEQID_PARCEL_SELECTED = -10000;

        public RequestResultType RequestResult;
        public int SequenceID;
        public bool SnapSelection;
        public int SelfCount;
        public int OtherCount;
        public int PublicCount;
        public int LocalID;
        public UUID OwnerID;
        public bool IsGroupOwned;
        public uint AuctionID;
        public Date ClaimDate;
        public int ClaimPrice;
        public int RentPrice;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public byte[] Bitmap = new byte[0];
        public int Area;
        public ParcelStatus Status;
        public int SimWideMaxPrims;
        public int SimWideTotalPrims;
        public int MaxPrims;
        public int TotalPrims;
        public int OwnerPrims;
        public int GroupPrims;
        public int OtherPrims;
        public int SelectedPrims;
        public double ParcelPrimBonus;
        public int OtherCleanTime;
        public ParcelFlags ParcelFlags;
        public int SalePrice;
        public string Name;
        public string Description;
        public string MusicURL;
        public string MediaURL;
        public UUID MediaID;
        public bool MediaAutoScale;
        public UUID GroupID;
        public int PassPrice;
        public double PassHours;
        public ParcelCategory Category;
        public UUID AuthBuyerID;
        public UUID SnapshotID;
        public Vector3 UserLocation;
        public Vector3 UserLookAt;
        public TeleportLandingType LandingType;
        public bool RegionPushOverride;
        public bool RegionDenyAnonymous;
        public bool RegionDenyIdentified;
        public bool RegionDenyTransacted;
        public bool RegionDenyAgeUnverified;
        public bool RegionAllowAccessOverride;

        public bool Privacy;
        public bool SeeAVs;
        public bool AnyAVSounds;
        public bool GroupAVSounds;

        public string MediaDesc;
        public int MediaWidth;
        public int MediaHeight;
        public bool MediaLoop;
        public string MediaType;
        public bool ObscureMedia;
        public bool ObscureMusic;

        public override MessageType Number => MessageType.ParcelProperties;

        public override IValue SerializeEQG()
        {
            var m = new MapType();
            byte[] parcelFlags = BitConverter.GetBytes((uint)ParcelFlags);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(parcelFlags);
            }
            m.Add("ParcelData", new AnArray
            {
                new MapType
                {
                    { "LocalID", LocalID },
                    { "AABBMax", AABBMax },
                    { "AABBMin", AABBMin },
                    { "Area", Area },
                    { "AuctionID", (int)AuctionID },
                    { "AuthBuyerID", AuthBuyerID },
                    { "Bitmap", new BinaryData(Bitmap) },
                    { "Category", (int)Category },
                    { "ClaimDate", ClaimDate },
                    { "ClaimPrice", ClaimPrice },
                    { "Desc", Description },
                    { "ParcelFlags", new BinaryData(parcelFlags) },
                    { "GroupID", GroupID },
                    { "GroupPrims", GroupPrims },
                    { "IsGroupOwned", IsGroupOwned },
                    { "LandingType", (int)LandingType },
                    { "MaxPrims", MaxPrims },
                    { "MediaID", MediaID },
                    { "MediaURL", MediaURL },
                    { "MediaAutoScale", MediaAutoScale },
                    { "MusicURL", MusicURL },
                    { "Name", Name },
                    { "OtherCleanTime", OtherCleanTime },
                    { "OtherCount", OtherCount },
                    { "OtherPrims", OtherPrims },
                    { "OwnerID", OwnerID },
                    { "OwnerPrims", OwnerPrims },
                    { "ParcelPrimBonus", ParcelPrimBonus },
                    { "PassHours", PassHours },
                    { "PassPrice", PassPrice },
                    { "PublicCount", PublicCount },
                    { "Privacy", Privacy },
                    { "RegionDenyAnonymous", RegionDenyAnonymous },
                    { "RegionDenyIdentified", RegionDenyIdentified },
                    { "RegionDenyTransacted", RegionDenyTransacted },
                    { "RegionPushOverride", RegionPushOverride },
                    { "RentPrice", RentPrice },
                    { "RequestResult", (int)RequestResult },
                    { "SalePrice", SalePrice },
                    { "SelectedPrims", SelectedPrims },
                    { "SelfCount", SelfCount },
                    { "SequenceID", SequenceID },
                    { "SimWideMaxPrims", SimWideMaxPrims },
                    { "SimWideTotalPrims", SimWideTotalPrims },
                    { "SnapSelection", SnapSelection },
                    { "SnapshotID", SnapshotID },
                    { "Status", (int)Status },
                    { "TotalPrims", TotalPrims },
                    { "UserLocation", UserLocation },
                    { "UserLookAt", UserLookAt },
                    { "SeeAVs", SeeAVs },
                    { "AnyAVSounds", AnyAVSounds },
                    { "GroupAVSounds", GroupAVSounds }
                }
            });
            m.Add("MediaData", new AnArray
            {
                new MapType
                {
                    { "MediaDesc", MediaDesc },
                    { "MediaHeight", MediaHeight },
                    { "MediaWidth", MediaWidth },
                    { "MediaLoop", MediaLoop },
                    { "MediaType", MediaType },
                    { "ObscureMedia", ObscureMedia },
                    { "ObscureMusic", ObscureMusic }
                }
            });
            m.Add("AgeVerificationBlock", new AnArray
            {
                new MapType
                {
                    { "RegionDenyAgeUnverified", RegionDenyAgeUnverified }
                }
            });
            m.Add("RegionAllowAccessBlock", new AnArray
            {
                new MapType
                {
                    { "RegionAllowAccessOverride", RegionAllowAccessOverride }
                }
            });

            return m;
        }

        public static Message DeserializeEQG(IValue iv)
        {
            var m = (MapType)iv;
            var parcelData = (MapType)((AnArray)m["ParcelData"])[0];
            var mediaData = (MapType)((AnArray)m["MediaData"])[0];
            var ageData = (MapType)((AnArray)m["AgeVerificationBlock"])[0];
            AnArray allowAccess;
            bool regionAllowAccessOverride = false;
            if(m.TryGetValue("RegionAllowAccessBlock", out allowAccess))
            {
                regionAllowAccessOverride = ((MapType)allowAccess[0])["RegionAllowAccessOverride"].AsBoolean;
            }
            byte[] parcelflagsdata = (BinaryData)parcelData["ParcelFlags"];
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(parcelflagsdata);
            }
            return new ParcelProperties
            {
                LocalID = parcelData["LocalID"].AsInt,
                AABBMax = parcelData["AABBMax"].AsVector3,
                AABBMin = parcelData["AABBMin"].AsVector3,
                Area = parcelData["Area"].AsInt,
                AuctionID = parcelData["AuctionID"].AsUInt,
                AuthBuyerID = parcelData["AuthBuyerID"].AsUUID,
                Bitmap = (BinaryData)parcelData["Bitmap"],
                Category = (ParcelCategory)parcelData["Category"].AsInt,
                ClaimDate = (Date)parcelData["ClaimDate"],
                ClaimPrice = parcelData["ClaimPrice"].AsInt,
                Description = parcelData["Desc"].ToString(),
                ParcelFlags = (ParcelFlags)BitConverter.ToUInt32(parcelflagsdata, 0),
                GroupID = parcelData["GroupID"].AsUUID,
                GroupPrims = parcelData["GroupPrims"].AsInt,
                IsGroupOwned = parcelData["IsGroupOwned"].AsBoolean,
                LandingType = (TeleportLandingType)parcelData["LandingType"].AsInt,
                MaxPrims = parcelData["MaxPrims"].AsInt,
                MediaID = parcelData["MediaID"].AsUUID,
                MediaURL = parcelData["MediaURL"].ToString(),
                MediaAutoScale = parcelData["MediaAutoScale"].AsBoolean,
                MusicURL = parcelData["MusicURL"].ToString(),
                Name = parcelData["Name"].ToString(),
                OtherCleanTime = parcelData["OtherCleanTim"].AsInt,
                OtherCount = parcelData["OtherCount"].AsInt,
                OtherPrims = parcelData["OtherPrims"].AsInt,
                OwnerID = parcelData["OwnerID"].AsUUID,
                OwnerPrims = parcelData["OwnerPrims"].AsInt,
                ParcelPrimBonus = parcelData["ParcelPrimBonus"].AsReal,
                PassHours = parcelData["PassHours"].AsReal,
                PassPrice = parcelData["PassPrice"].AsInt,
                PublicCount = parcelData["PublicCount"].AsInt,
                Privacy = parcelData["Privacy"].AsBoolean,
                RegionDenyAnonymous = parcelData["RegionDenyAnonymous"].AsBoolean,
                RegionDenyIdentified = parcelData["RegionDenyIdentified"].AsBoolean,
                RegionPushOverride = parcelData["RegionPushOverride"].AsBoolean,
                RegionAllowAccessOverride = regionAllowAccessOverride,
                RentPrice = parcelData["RentPrice"].AsInt,
                RequestResult = (RequestResultType)parcelData["RequestResult"].AsInt,
                SalePrice = parcelData["SalePrice"].AsInt,
                SelectedPrims = parcelData["SelectedPrims"].AsInt,
                SelfCount = parcelData["SelfCount"].AsInt,
                SequenceID = parcelData["SequenceID"].AsInt,
                SimWideMaxPrims = parcelData["SimWideMaxPrims"].AsInt,
                SimWideTotalPrims = parcelData["SimWideTotalPrims"].AsInt,
                SnapSelection = parcelData["SnapSelection"].AsBoolean,
                SnapshotID = parcelData["SnapshotID"].AsUUID,
                Status = (ParcelStatus)parcelData["Status"].AsInt,
                TotalPrims = parcelData["TotalPrims"].AsInt,
                UserLocation = parcelData["UserLocation"].AsVector3,
                UserLookAt = parcelData["UserLookAt"].AsVector3,
                SeeAVs = parcelData["SeeAVs"].AsBoolean,
                AnyAVSounds = parcelData["AnyAVSounds"].AsBoolean,
                GroupAVSounds = parcelData["GroupAVSounds"].AsBoolean,
                MediaDesc = mediaData["MediaDesc"].ToString(),
                MediaWidth = mediaData["MediaWidth"].AsInt,
                MediaHeight = mediaData["MediaHeight"].AsInt,
                MediaLoop = mediaData["MediaLoop"].AsBoolean,
                MediaType = mediaData["MediaType"].ToString(),
                ObscureMedia = mediaData["ObscureMedia"].AsBoolean,
                ObscureMusic = mediaData["ObscureMusic"].AsBoolean,
                RegionDenyAgeUnverified = ageData["RegionDenyAgeUnverified"].AsBoolean
            };
        }
    }
}
