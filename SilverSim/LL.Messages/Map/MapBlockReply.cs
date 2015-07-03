/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
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
        public UInt32 Flags;

        public struct DataEntry
        {
            public UInt16 X;
            public UInt16 Y;
            public string Name;
            public byte Access;
            public UInt32 RegionFlags;
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
            p.WriteUInt32(Flags);
            p.WriteUInt8((byte)Data.Count);
            foreach (DataEntry d in Data)
            {
                p.WriteUInt16(d.X);
                p.WriteUInt16(d.Y);
                p.WriteStringLen8(d.Name);
                p.WriteUInt8(d.Access);
                p.WriteUInt32(d.RegionFlags);
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
