using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArribaSim.Linden.Messages.Agent
{
    public class HealthMessage : Message
    {
        public double Health = 0f;

        public HealthMessage()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.HealthMessage;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteFloat((float)Health);
        }
    }
}
