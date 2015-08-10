// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Names
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
            p.WriteMessageType(Number);
            p.WriteUInt8((byte)UUIDNameBlock.Count);
            foreach(Data d in UUIDNameBlock)
            {
                p.WriteUUID(d.ID);
                p.WriteStringLen8(d.FirstName + "\0");
                p.WriteStringLen8(d.LastName + "\0");
            }
        }
    }
}
