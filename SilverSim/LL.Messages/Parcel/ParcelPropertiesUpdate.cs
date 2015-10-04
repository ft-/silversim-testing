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

        public Int32 LocalID = 0;
        public UInt32 Flags = 0;
        public ParcelFlags ParcelFlags = ParcelFlags.None;
        public Int32 SalePrice = 0;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string MusicURL = string.Empty;
        public string MediaURL = string.Empty;
        public UUID MediaID = UUID.Zero;
        public bool MediaAutoScale = false;
        public UUID GroupID = UUID.Zero;
        public Int32 PassPrice = 0;
        public double PassHours = 0;
        public ParcelCategory Category = 0;
        public UUID AuthBuyerID = UUID.Zero;
        public UUID SnapshotID = UUID.Zero;
        public Vector3 UserLocation = Vector3.Zero;
        public Vector3 UserLookAt = Vector3.Zero;
        public TeleportLandingType LandingType = 0;

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
            m.UserLocation.X = p.ReadFloat();
            m.UserLocation.Y = p.ReadFloat();
            m.UserLocation.Z = p.ReadFloat();
            m.UserLookAt.X = p.ReadFloat();
            m.UserLookAt.Y = p.ReadFloat();
            m.UserLookAt.Z = p.ReadFloat();
            m.LandingType = (TeleportLandingType)p.ReadUInt8();
            return m;
        }
    }
}
