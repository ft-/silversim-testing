/*

ArribaSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using ArribaSim.Types;
using System;

namespace ArribaSim.LL.Messages.Parcel
{
    public class ParcelProperties : Message
    {
        public Int32 RequestResult;
        public Int32 SequenceID;
        public bool SnapSelection;
        public Int32 SelfCount;
        public Int32 OtherCount;
        public Int32 PublicCount;
        public Int32 LocalID;
        public UUID OwnerID;
        public bool IsGroupOwned;
        public UInt32 AuctionID;
        public Int32 ClaimDate;
        public Int32 ClaimPrice;
        public Int32 RentPrice;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public byte[] Bitmap = new byte[0];
        public Int32 Area;
        public byte Status;
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
        public UInt32 ParcelFlags;
        public Int32 SalePrice;
        public string Name;
        public string Desc;
        public string MusicURL;
        public string MediaURL;
        public UUID MediaID;
        public byte MediaAutoScale;
        public UUID GroupID;
        public Int32 PassPrice;
        public double PassHours;
        public byte Category;
        public UUID AuthBuyerID;
        public UUID SnapshotID;
        public Vector3 UserLocation;
        public Vector3 UserLookAt;
        public byte LandingType;
        public bool RegionPushOverride;
        public bool RegionDenyAnonymous;
        public bool RegionDenyIdentified;
        public bool RegionDenyTransacted;
        public bool RegionDenyAgeUnverified;

        public ParcelProperties()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ParcelProperties;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteInt32(RequestResult);
            p.WriteInt32(SequenceID);
            p.WriteBoolean(SnapSelection);
            p.WriteInt32(SelfCount);
            p.WriteInt32(OtherCount);
            p.WriteInt32(PublicCount);
            p.WriteInt32(LocalID);
            p.WriteUUID(OwnerID);
            p.WriteBoolean(IsGroupOwned);
            p.WriteUInt32(AuctionID);
            p.WriteInt32(ClaimDate);
            p.WriteInt32(ClaimPrice);
            p.WriteInt32(RentPrice);
            p.WriteVector3f(AABBMin);
            p.WriteVector3f(AABBMax);
            p.WriteUInt16BE((UInt16)Bitmap.Length);
            p.WriteBytes(Bitmap);
            p.WriteInt32(Area);
            p.WriteUInt8(Status);
            p.WriteInt32(SimWideMaxPrims);
            p.WriteInt32(SimWideTotalPrims);
            p.WriteInt32(MaxPrims);
            p.WriteInt32(TotalPrims);
            p.WriteInt32(OwnerPrims);
            p.WriteInt32(GroupPrims);
            p.WriteInt32(OtherPrims);
            p.WriteInt32(SelectedPrims);
            p.WriteFloat((float)ParcelPrimBonus);
            p.WriteInt32(OtherCleanTime);
            p.WriteUInt32(ParcelFlags);
            p.WriteInt32(SalePrice);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Desc);
            p.WriteStringLen8(MusicURL);
            p.WriteStringLen8(MediaURL);
            p.WriteUUID(MediaID);
            p.WriteUInt8(MediaAutoScale);
            p.WriteUUID(GroupID);
            p.WriteInt32(PassPrice);
            p.WriteFloat((float)PassHours);
            p.WriteUInt8(Category);
            p.WriteUUID(AuthBuyerID);
            p.WriteUUID(SnapshotID);
            p.WriteVector3f(UserLocation);
            p.WriteVector3f(UserLookAt);
            p.WriteUInt8(LandingType);
            p.WriteBoolean(RegionPushOverride);
            p.WriteBoolean(RegionDenyAnonymous);
            p.WriteBoolean(RegionDenyIdentified);
            p.WriteBoolean(RegionDenyTransacted);
            p.WriteBoolean(RegionDenyAgeUnverified);
        }
    }
}
