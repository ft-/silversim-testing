// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Names
{
    [UDPMessage(MessageType.UUIDGroupNameReply)]
    [Reliable]
    [Trusted]
    public class UUIDGroupNameReply : Message
    {
        public struct Data
        {
            public UUID ID;
            public string GroupName;

            public Data(UGI group)
            {
                ID = group.ID;
                GroupName = group.GroupName;
            }
        }

        public List<Data> UUIDNameBlock = new List<Data>();

        public UUIDGroupNameReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)UUIDNameBlock.Count);
            foreach(Data d in UUIDNameBlock)
            {
                p.WriteUUID(d.ID);
                p.WriteStringLen8(d.GroupName);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            UUIDGroupNameReply m = new UUIDGroupNameReply();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                Data d = new Data();
                d.ID = p.ReadUUID();
                d.GroupName = p.ReadStringLen8();
                m.UUIDNameBlock.Add(d);
            }
            return m;
        }
    }
}
