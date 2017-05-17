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

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelPropertiesUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelPropertiesUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID;
        public UInt32 Flags;
        public ParcelFlags ParcelFlags;
        public Int32 SalePrice;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string MusicURL = string.Empty;
        public string MediaURL = string.Empty;
        public UUID MediaID = UUID.Zero;
        public bool MediaAutoScale;
        public UUID GroupID = UUID.Zero;
        public Int32 PassPrice;
        public double PassHours;
        public ParcelCategory Category;
        public UUID AuthBuyerID = UUID.Zero;
        public UUID SnapshotID = UUID.Zero;
        public Vector3 UserLocation = Vector3.Zero;
        public Vector3 UserLookAt = Vector3.Zero;
        public TeleportLandingType LandingType;

        public static Message Decode(UDPPacket p)
        {
            return new ParcelPropertiesUpdate()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                LocalID = p.ReadInt32(),
                Flags = p.ReadUInt32(),
                ParcelFlags = (ParcelFlags)p.ReadUInt32(),
                SalePrice = p.ReadInt32(),
                Name = p.ReadStringLen8(),
                Description = p.ReadStringLen8(),
                MusicURL = p.ReadStringLen8(),
                MediaURL = p.ReadStringLen8(),
                MediaAutoScale = p.ReadBoolean(),
                GroupID = p.ReadUUID(),
                PassPrice = p.ReadInt32(),
                PassHours = p.ReadFloat(),
                Category = (ParcelCategory)p.ReadUInt8(),
                AuthBuyerID = p.ReadUUID(),
                SnapshotID = p.ReadUUID(),
                UserLocation = p.ReadVector3f(),
                UserLookAt = p.ReadVector3f(),
                LandingType = (TeleportLandingType)p.ReadUInt8()
            };
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteInt32(LocalID);
            p.WriteUInt32(Flags);
            p.WriteUInt32((uint)ParcelFlags);
            p.WriteInt32(SalePrice);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
            p.WriteStringLen8(MusicURL);
            p.WriteStringLen8(MediaURL);
            p.WriteBoolean(MediaAutoScale);
            p.WriteUUID(GroupID);
            p.WriteInt32(PassPrice);
            p.WriteFloat((float)PassHours);
            p.WriteUInt8((byte)Category);
            p.WriteUUID(AuthBuyerID);
            p.WriteUUID(SnapshotID);
            p.WriteVector3f(UserLocation);
            p.WriteVector3f(UserLookAt);
            p.WriteUInt8((byte)LandingType);
        }
    }
}
