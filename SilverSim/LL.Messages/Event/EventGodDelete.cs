// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Event
{
    [UDPMessage(MessageType.EventGodDelete)]
    [NotTrusted]
    public class EventGodDelete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 EventID;
        public UUID QueryID;
        public string QueryText;
        public UInt32 QueryFlags;
        public Int32 QueryStart;

        public EventGodDelete()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            EventGodDelete m = new EventGodDelete();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.EventID = p.ReadUInt32();
            m.QueryID = p.ReadUUID();
            m.QueryText = p.ReadStringLen8();
            m.QueryFlags = p.ReadUInt32();
            m.QueryStart = p.ReadInt32();

            return m;
        }
    }
}
