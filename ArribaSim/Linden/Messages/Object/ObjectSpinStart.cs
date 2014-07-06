using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Linden.Messages.Object
{
    public class ObjectSpinStart : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID ObjectID = UUID.Zero;

        public ObjectSpinStart()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ObjectSpinStart;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ObjectSpinStart m = new ObjectSpinStart();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            return m;
        }
    }
}
