using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Linden.Messages.Object
{
    public class ObjectSpinStop : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID ObjectID = UUID.Zero;

        public ObjectSpinStop()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ObjectSpinStop;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ObjectSpinStop m = new ObjectSpinStop();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            return m;
        }
    }
}
