// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Event
{
    [UDPMessage(MessageType.EventInfoRequest)]
    [Reliable]
    [NotTrusted]
    public class EventInfoRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 EventID;

        public EventInfoRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            EventInfoRequest m = new EventInfoRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.EventID = p.ReadUInt32();

            return m;
        }
    }
}
