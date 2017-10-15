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
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Map
{
    [UDPMessage(MessageType.MapLayerReply)]
    [Reliable]
    [Trusted]
    public class MapLayerReply : Message
    {
        public UUID AgentID;
        public MapAgentFlags Flags;

        public struct LayerDataEntry
        {
            public UInt32 Left;
            public UInt32 Right;
            public UInt32 Top;
            public UInt32 Bottom;
            public UUID ImageID;
        }

        public List<LayerDataEntry> LayerData = new List<LayerDataEntry>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt32((uint)Flags);
            p.WriteUInt8((byte)LayerData.Count);
            foreach (var d in LayerData)
            {
                p.WriteUInt32(d.Left);
                p.WriteUInt32(d.Right);
                p.WriteUInt32(d.Top);
                p.WriteUInt32(d.Bottom);
                p.WriteUUID(d.ImageID);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new MapLayerReply
            {
                AgentID = p.ReadUUID(),
                Flags = (MapAgentFlags)p.ReadUInt32()
            };
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.LayerData.Add(new LayerDataEntry
                {
                    Left = p.ReadUInt32(),
                    Right = p.ReadUInt32(),
                    Top = p.ReadUInt32(),
                    Bottom = p.ReadUInt32(),
                    ImageID = p.ReadUUID()
                });
            }
            return m;
        }
    }
}
