// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Script
{
    [UDPMessage(MessageType.ScriptQuestion)]
    [Reliable]
    public class ScriptQuestion : Message
    {
        public UUID TaskID = UUID.Zero;
        public UUID ItemID = UUID.Zero;
        public string ObjectName;
        public string ObjectOwner;
        public UInt32 Questions;
        public UUID ExperienceID = UUID.Zero;

        public ScriptQuestion()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(TaskID);
            p.WriteUUID(ItemID);
            p.WriteStringLen8(ObjectName);
            p.WriteStringLen8(ObjectOwner);
            p.WriteUInt32(Questions);
            p.WriteUUID(ExperienceID);
        }
    }
}
