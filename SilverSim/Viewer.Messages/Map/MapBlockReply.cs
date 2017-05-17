// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Map
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
            public RegionOptionFlags RegionFlags;
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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt32((uint)Flags);
            p.WriteUInt8((byte)Data.Count);
            foreach (var d in Data)
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
            foreach(var d in Size)
            {
                p.WriteUInt16(d.X);
                p.WriteUInt16(d.Y);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            MapBlockReply m = new MapBlockReply();
            m.AgentID = p.ReadUUID();
            m.Flags = (MapAgentFlags)p.ReadUInt32();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                DataEntry d = new DataEntry();
                d.X = p.ReadUInt16();
                d.Y = p.ReadUInt16();
                d.Name = p.ReadStringLen8();
                d.Access = (RegionAccess)p.ReadUInt8();
                d.RegionFlags = (RegionOptionFlags)p.ReadUInt32();
                d.WaterHeight = p.ReadUInt8();
                d.Agents = p.ReadUInt8();
                d.MapImageID = p.ReadUUID();
                m.Data.Add(d);
            }
            n = p.ReadUInt8();
            while(n--!=0)
            {
                SizeInfo d = new SizeInfo();
                d.X = p.ReadUInt16();
                d.Y = p.ReadUInt16();
                m.Size.Add(d);
            }
            return m;
        }
    }
}
