// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

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

        public MapLayerReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt32((uint)Flags);
            p.WriteUInt8((byte)LayerData.Count);
            foreach (LayerDataEntry d in LayerData)
            {
                p.WriteUInt32(d.Left);
                p.WriteUInt32(d.Right);
                p.WriteUInt32(d.Top);
                p.WriteUInt32(d.Bottom);
                p.WriteUUID(d.ImageID);
            }
        }
    }
}
