// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Map
{
    [UDPMessage(MessageType.MapItemReply)]
    [Reliable]
    [Trusted]
    public class MapItemReply : Message
    {
        public UUID AgentID;
        public MapAgentFlags Flags;
        public MapItemType ItemType;

        public struct DataEntry
        {
            public UInt16 X;
            public UInt16 Y;
            public UUID ID;
            public Int32 Extra;
            public Int32 Extra2;
            public string Name;
        }

        public List<DataEntry> Data = new List<DataEntry>();

        public MapItemReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt32((uint)Flags);
            p.WriteUInt32((uint)ItemType);
            p.WriteUInt8((byte)Data.Count);
            foreach (DataEntry d in Data)
            {
                p.WriteUInt32(d.X);
                p.WriteUInt16(d.Y);
                p.WriteUUID(d.ID);
                p.WriteInt32(d.Extra);
                p.WriteInt32(d.Extra2);
                p.WriteStringLen8(d.Name);
            }
        }
    }
}
