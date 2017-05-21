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
    [UDPMessage(MessageType.AvatarInterestsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class AvatarInterestsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID AvatarID = UUID.Zero;
        public UInt32 WantToMask;
        public string WantToText = string.Empty;
        public UInt32 SkillsMask;
        public string SkillsText = string.Empty;
        public string LanguagesText = string.Empty;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(AvatarID);
            p.WriteUInt32(WantToMask);
            p.WriteStringLen8(WantToText);
            p.WriteUInt32(SkillsMask);
            p.WriteStringLen8(SkillsText);
            p.WriteStringLen8(LanguagesText);
        }

        public static Message Decode(UDPPacket p) => new AvatarInterestsReply()
        {
            AgentID = p.ReadUUID(),
            AvatarID = p.ReadUUID(),
            WantToMask = p.ReadUInt32(),
            WantToText = p.ReadStringLen8(),
            SkillsMask = p.ReadUInt32(),
            SkillsText = p.ReadStringLen8(),
            LanguagesText = p.ReadStringLen8()
        };
    }
}
