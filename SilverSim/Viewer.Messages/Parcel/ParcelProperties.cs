// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using MapType = SilverSim.Types.Map;
using SilverSim.Types.Parcel;
using System;

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
        public RequestResultType RequestResult;
        public Int32 SequenceID;
        public bool SnapSelection;
        public Int32 SelfCount;
        public Int32 OtherCount;
        public Int32 PublicCount;
        public Int32 LocalID;
        public UUID OwnerID;
        public bool IsGroupOwned;
        public UInt32 AuctionID;
        public Date ClaimDate;
        public Int32 ClaimPrice;
        public Int32 RentPrice;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public byte[] Bitmap = new byte[0];
        public Int32 Area;
        public ParcelStatus Status;
        public Int32 SimWideMaxPrims;
        public Int32 SimWideTotalPrims;
        public Int32 MaxPrims;
        public Int32 TotalPrims;
        public Int32 OwnerPrims;
        public Int32 GroupPrims;
        public Int32 OtherPrims;
        public Int32 SelectedPrims;
        public double ParcelPrimBonus;
        public Int32 OtherCleanTime;
        public ParcelFlags ParcelFlags;
        public Int32 SalePrice;
        public string Name;
        public string Description;
        public string MusicURL;
        public string MediaURL;
        public UUID MediaID;
        public bool MediaAutoScale;
        public UUID GroupID;
        public Int32 PassPrice;
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

        public ParcelProperties()
        {

        }

        public override MessageType Number
        {
            get
            {
                return 0;
            }
        }

        public override IValue SerializeEQG()
        {
            MapType m = new MapType();
            AnArray parcelArray = new AnArray();
            MapType parcelData = new MapType();
            parcelData.Add("LocalID", LocalID);
            parcelData.Add("AABBMax", AABBMax);
            parcelData.Add("AABBMin", AABBMin);
            parcelData.Add("Area", Area);
            parcelData.Add("AuctionID", AuctionID);
            parcelData.Add("AuthBuyerID", AuthBuyerID);
            parcelData.Add("Bitmap", new BinaryData(Bitmap));
            parcelData.Add("Category", (int)Category);
            parcelData.Add("ClaimDate", ClaimDate.AsInt);
            parcelData.Add("ClaimPrice", ClaimPrice);
            parcelData.Add("Desc", Description);
            parcelData.Add("ParcelFlags", (byte)ParcelFlags);
            parcelData.Add("GroupID", GroupID);
            parcelData.Add("GroupPrims", GroupPrims);
            parcelData.Add("IsGroupOwned", IsGroupOwned);
            parcelData.Add("LandingType", (int)LandingType);
            parcelData.Add("MaxPrims", MaxPrims);
            parcelData.Add("MediaID", MediaID);
            parcelData.Add("MediaURL", MediaURL);
            parcelData.Add("MediaAutoScale", MediaAutoScale);
            parcelData.Add("MusicURL", MusicURL);
            parcelData.Add("Name", Name);
            parcelData.Add("OtherCleanTime", OtherCleanTime);
            parcelData.Add("OtherCount", OtherCount);
            parcelData.Add("OtherPrims", OtherPrims);
            parcelData.Add("ParcelPrimBonus", ParcelPrimBonus);
            parcelData.Add("PassHours", PassHours);
            parcelData.Add("PassPrice", PassPrice);
            parcelData.Add("PublicCount", PublicCount);
            parcelData.Add("Privacy", Privacy);
            parcelData.Add("RegionDenyAnonymous", RegionDenyAnonymous);
            parcelData.Add("RegionDenyIdentified", RegionDenyIdentified);
            parcelData.Add("RegionDenyTransacted", RegionDenyTransacted);
            parcelData.Add("RegionPushOverride", RegionPushOverride);
            parcelData.Add("RentPrice", RentPrice);
            parcelData.Add("RequestResult", (int)RequestResult);
            parcelData.Add("SalePrice", SalePrice);
            parcelData.Add("SelectedPrims", SelectedPrims);
            parcelData.Add("SelfCount", SelfCount);
            parcelData.Add("SequenceID", SequenceID);
            parcelData.Add("SimWideMaxPrims", SimWideMaxPrims);
            parcelData.Add("SimWideTotalPrims", SimWideTotalPrims);
            parcelData.Add("SnapSelection", SnapSelection);
            parcelData.Add("SnapshotID", SnapshotID);
            parcelData.Add("Status", (int)Status);
            parcelData.Add("TotalPrims", TotalPrims);
            parcelData.Add("UserLocation", UserLocation);
            parcelData.Add("UserLookAt", UserLookAt);
            parcelData.Add("SeeAVs", SeeAVs);
            parcelData.Add("AnyAVSounds", AnyAVSounds);
            parcelData.Add("GroupAVSounds", GroupAVSounds);
            parcelArray.Add(parcelData);
            m.Add("ParcelData", parcelArray);

            AnArray mediaArray = new AnArray();
            MapType mediaData = new MapType();
            mediaData.Add("MediaDesc", MediaDesc);
            mediaData.Add("MediaHeight", MediaHeight);
            mediaData.Add("MediaWidth", MediaWidth);
            mediaData.Add("MediaLoop", MediaLoop);
            mediaData.Add("MediaType", MediaType);
            mediaData.Add("ObscureMedia", ObscureMedia);
            mediaData.Add("ObscureMusic", ObscureMusic);
            mediaArray.Add(mediaData);
            m.Add("MediaData", mediaArray);

            AnArray ageArray = new AnArray();
            MapType ageData = new MapType();
            ageData.Add("RegionDenyAgeUnverified", RegionDenyAgeUnverified);
            ageArray.Add(ageData);
            m.Add("AgeVerificationBlock", ageArray);
            return m;
        }
    }
}
