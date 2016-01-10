// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarPropertiesReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class AvatarPropertiesReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID AvatarID = UUID.Zero;
        public UUID ImageID = UUID.Zero;
        public UUID FLImageID = UUID.Zero;
        public UUID PartnerID = UUID.Zero;
        public string AboutText = string.Empty;
        public string FLAboutText = string.Empty;
        public string BornOn = string.Empty;
        public string ProfileURL = string.Empty;
        public byte[] CharterMember = new byte[1];
        public UInt32 Flags;

        public AvatarPropertiesReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(AvatarID);
            p.WriteUUID(ImageID);
            p.WriteUUID(FLImageID);
            p.WriteUUID(PartnerID);
            p.WriteStringLen16(AboutText);
            p.WriteStringLen8(FLAboutText);
            p.WriteStringLen8(BornOn);
            p.WriteStringLen8(ProfileURL);
            p.WriteUInt8((byte)CharterMember.Length);
            p.WriteBytes(CharterMember);
            p.WriteUInt32(Flags);
        }

        public static Message Decode(UDPPacket p)
        {
            AvatarPropertiesReply m = new AvatarPropertiesReply();
            m.AgentID = p.ReadUUID();
            m.AvatarID = p.ReadUUID();
            m.ImageID = p.ReadUUID();
            m.FLImageID = p.ReadUUID();
            m.PartnerID = p.ReadUUID();
            m.AboutText = p.ReadStringLen16();
            m.FLAboutText = p.ReadStringLen8();
            m.BornOn = p.ReadStringLen8();
            m.ProfileURL = p.ReadStringLen8();
            m.CharterMember = p.ReadBytes(p.ReadUInt8());
            m.Flags = p.ReadUInt32();
            return m;
        }
    }
}
