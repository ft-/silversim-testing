// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.KillObject)]
    [Reliable]
    [Trusted]
    public class KillObject : Message
    {
        public List<UInt32> LocalIDs = new List<UInt32>();

        public KillObject()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)LocalIDs.Count);
            foreach (UInt32 i in LocalIDs)
            {
                p.WriteUInt32(i);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            KillObject m = new KillObject();
            uint n = p.ReadUInt8();
            while (n-- != 0)
            {
                m.LocalIDs.Add(p.ReadUInt32());
            }
            return m;
        }
    }
}
