using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Linden.Messages.Object
{
    public class KillObject : Message
    {
        public List<UInt32> LocalIDs = new List<UInt32>();

        public KillObject()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.KillObject;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt8((byte)LocalIDs.Count);
            foreach (UInt32 i in LocalIDs)
            {
                p.WriteUInt32(i);
            }
        }
    }
}
