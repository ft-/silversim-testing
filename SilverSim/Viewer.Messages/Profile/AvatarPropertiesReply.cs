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
            return new AvatarPropertiesReply()
            {
                AgentID = p.ReadUUID(),
                AvatarID = p.ReadUUID(),
                ImageID = p.ReadUUID(),
                FLImageID = p.ReadUUID(),
                PartnerID = p.ReadUUID(),
                AboutText = p.ReadStringLen16(),
                FLAboutText = p.ReadStringLen8(),
                BornOn = p.ReadStringLen8(),
                ProfileURL = p.ReadStringLen8(),
                CharterMember = p.ReadBytes(p.ReadUInt8()),
                Flags = p.ReadUInt32()
            };
        }
    }
}
