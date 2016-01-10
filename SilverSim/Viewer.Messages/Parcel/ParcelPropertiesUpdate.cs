// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public ParcelPropertiesUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelPropertiesUpdate m = new ParcelPropertiesUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.Flags = p.ReadUInt32();
            m.ParcelFlags = (ParcelFlags)p.ReadUInt32();
            m.SalePrice = p.ReadInt32();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();
            m.MusicURL = p.ReadStringLen8();
            m.MediaURL = p.ReadStringLen8();
            m.MediaAutoScale = p.ReadBoolean();
            m.GroupID = p.ReadUUID();
            m.PassPrice = p.ReadInt32();
            m.PassHours = p.ReadFloat();
            m.Category = (ParcelCategory)p.ReadUInt8();
            m.AuthBuyerID = p.ReadUUID();
            m.SnapshotID = p.ReadUUID();
            m.UserLocation = p.ReadVector3f();
            m.UserLookAt = p.ReadVector3f();
            m.LandingType = (TeleportLandingType)p.ReadUInt8();
            return m;
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
