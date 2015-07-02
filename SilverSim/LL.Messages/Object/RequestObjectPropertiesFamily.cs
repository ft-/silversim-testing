using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.RequestObjectPropertiesFamily)]
    [Reliable]
    public class RequestObjectPropertiesFamily : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UInt32 RequestFlags = 0;
        public UUID ObjectID = UUID.Zero;

        public RequestObjectPropertiesFamily()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestObjectPropertiesFamily m = new RequestObjectPropertiesFamily();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.RequestFlags = p.ReadUInt32();
            m.ObjectID = p.ReadUUID();
            return m;
        }
    }
}
