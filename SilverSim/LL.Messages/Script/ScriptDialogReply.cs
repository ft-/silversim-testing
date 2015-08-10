// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Script
{
    [UDPMessage(MessageType.ScriptDialogReply)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ScriptDialogReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID ObjectID = UUID.Zero;

        public Int32 ChatChannel = 0;
        public Int32 ButtonIndex = 0;
        public string ButtonLabel = "";

        public ScriptDialogReply()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ScriptDialogReply m = new ScriptDialogReply();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.ChatChannel = p.ReadInt32();
            m.ButtonIndex = p.ReadInt32();
            m.ButtonLabel = p.ReadStringLen8();
            return m;
        }
    }
}
