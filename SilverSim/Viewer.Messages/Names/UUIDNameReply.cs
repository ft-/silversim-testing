// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Names
{
    [UDPMessage(MessageType.UUIDNameReply)]
    [Reliable]
    [Trusted]
    public class UUIDNameReply : Message
    {
        public struct Data
        {
            public UUID ID;
            public string FirstName;
            public string LastName;
        }

        public List<Data> UUIDNameBlock = new List<Data>();

        public UUIDNameReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)UUIDNameBlock.Count);
            foreach(Data d in UUIDNameBlock)
            {
                p.WriteUUID(d.ID);
                p.WriteStringLen8(d.FirstName);
                p.WriteStringLen8(d.LastName);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            UUIDNameReply m = new UUIDNameReply();
            uint n = p.ReadUInt8();
            while (n-- != 0)
            {
                Data d = new Data();
                d.ID = p.ReadUUID();
                d.FirstName = p.ReadStringLen8();
                d.LastName = p.ReadStringLen8();
                m.UUIDNameBlock.Add(d);
            }
            return m;
        }
    }
}
