// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Event
{
    [UDPMessage(MessageType.EventLocationRequest)]
    [Reliable]
    [NotTrusted]
    public class EventLocationRequest : Message
    {
        public UUID QueryID;
        public UInt32 EventID;

        public EventLocationRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            EventLocationRequest m = new EventLocationRequest();
            m.QueryID = p.ReadUUID();
            m.EventID = p.ReadUInt32();

            return m;
        }
    }
}
