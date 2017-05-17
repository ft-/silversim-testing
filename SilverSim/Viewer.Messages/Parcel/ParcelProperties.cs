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

        public override MessageType Number
        {
            get
            {
                return 0;
            }
        }

        public override IValue SerializeEQG()
        {
            var m = new MapType();
            byte[] parcelFlags = BitConverter.GetBytes((uint)ParcelFlags);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(parcelFlags);
            }
            var parcelArray = new AnArray
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
            };
            m.Add("ParcelData", parcelArray);

            var mediaArray = new AnArray
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
            };
            m.Add("MediaData", mediaArray);

            var ageArray = new AnArray
            {
                new MapType
                {
                    { "RegionDenyAgeUnverified", RegionDenyAgeUnverified }
                }
            };
            m.Add("AgeVerificationBlock", ageArray);
            return m;
        }
    }
}
