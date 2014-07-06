using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Linden.Messages.Agent
{
    public class TrackAgent : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID PreyID = UUID.Zero;

        public TrackAgent()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.TrackAgent;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            TrackAgent m = new TrackAgent();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.PreyID = p.ReadUUID();
            return m;
        }
    }
}
