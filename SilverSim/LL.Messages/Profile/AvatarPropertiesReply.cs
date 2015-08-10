// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Profile
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
        public string AboutText;
        public string FLAboutText;
        public string BornOn;
        public string ProfileURL;
        public byte[] CharterMember = new byte[1];
        public UInt32 Flags = 0;

        public AvatarPropertiesReply()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.AvatarPropertiesReply;
            }
        }

        public override bool IsReliable
        {
            get
            {
                return true;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
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
    }
}
