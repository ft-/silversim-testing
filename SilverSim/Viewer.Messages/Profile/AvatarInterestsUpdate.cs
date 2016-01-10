// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarInterestsUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class AvatarInterestsUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 WantToMask;
        public string WantToText;
        public UInt32 SkillsMask;
        public string SkillsText;
        public string LanguagesText;

        public AvatarInterestsUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AvatarInterestsUpdate m = new AvatarInterestsUpdate();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.WantToMask = p.ReadUInt32();
            m.WantToText = p.ReadStringLen8();
            m.SkillsMask = p.ReadUInt32();
            m.SkillsText = p.ReadStringLen8();
            m.LanguagesText = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(WantToMask);
            p.WriteStringLen8(WantToText);
            p.WriteUInt32(SkillsMask);
            p.WriteStringLen8(SkillsText);
            p.WriteStringLen8(LanguagesText);
        }
    }
}
