// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Event
{
    [UDPMessage(MessageType.EventNotificationRemoveRequest)]
    [Reliable]
    [NotTrusted]
    public class EventNotificationRemoveRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 EventID;

        public EventNotificationRemoveRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            EventNotificationRemoveRequest m = new EventNotificationRemoveRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.EventID = p.ReadUInt32();

            return m;
        }
    }
}
