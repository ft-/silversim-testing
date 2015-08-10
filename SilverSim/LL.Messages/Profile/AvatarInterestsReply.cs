﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.AvatarInterestsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class AvatarInterestsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID AvatarID = UUID.Zero;
        public UInt32 WantToMask = 0;
        public string WantToText = string.Empty;
        public UInt32 SkillsMask = 0;
        public string SkillsText = string.Empty;
        public string LanguagesText = string.Empty;

        public AvatarInterestsReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(AvatarID);
            p.WriteUInt32(WantToMask);
            p.WriteStringLen8(WantToText);
            p.WriteUInt32(SkillsMask);
            p.WriteStringLen8(SkillsText);
            p.WriteStringLen8(LanguagesText);
        }
    }
}
