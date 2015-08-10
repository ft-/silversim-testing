// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Script
{
    [UDPMessage(MessageType.ScriptAnswerYes)]
    [Reliable]
    [NotTrusted]
    public class ScriptAnswerYes : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID TaskID = UUID.Zero;
        public UUID ItemID = UUID.Zero;
        public UInt32 Questions = 0;

        public ScriptAnswerYes()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ScriptAnswerYes m = new ScriptAnswerYes();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TaskID = p.ReadUUID();
            m.ItemID = p.ReadUUID();
            m.Questions = p.ReadUInt32();

            return m;
        }
    }
}
