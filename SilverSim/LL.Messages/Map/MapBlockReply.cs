// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Map
{
    [UDPMessage(MessageType.MapBlockReply)]
    [Reliable]
    [Trusted]
    public class MapBlockReply : Message
    {
        public UUID AgentID;
        public MapAgentFlags Flags;

        public struct DataEntry
        {
            public UInt16 X;
            public UInt16 Y;
            public string Name;
            public RegionAccess Access;
            public RegionFlags RegionFlags;
            public byte WaterHeight;
            public byte Agents;
            public UUID MapImageID;
        }

        public List<DataEntry> Data = new List<DataEntry>();

        public struct SizeInfo
        {
            public UInt16 X;
            public UInt16 Y;
        }

        public List<SizeInfo> Size = new List<SizeInfo>();

        public MapBlockReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUInt32((uint)Flags);
            p.WriteUInt8((byte)Data.Count);
            foreach (DataEntry d in Data)
            {
                p.WriteUInt16(d.X);
                p.WriteUInt16(d.Y);
                p.WriteStringLen8(d.Name);
                p.WriteUInt8((byte)d.Access);
                p.WriteUInt32((uint)d.RegionFlags);
                p.WriteUInt8(d.WaterHeight);
                p.WriteUInt8(d.Agents);
                p.WriteUUID(d.MapImageID);
            }
            p.WriteUInt8((byte)Size.Count);
            foreach(SizeInfo d in Size)
            {
                p.WriteUInt16(d.X);
                p.WriteUInt16(d.Y);
            }
        }
    }
}
