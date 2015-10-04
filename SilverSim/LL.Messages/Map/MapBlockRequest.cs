// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Map
{
    [UDPMessage(MessageType.MapBlockRequest)]
    [Reliable]
    [NotTrusted]
    public class MapBlockRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public MapAgentFlags Flags;
        public UInt32 EstateID;
        public bool IsGodlike;
        public GridVector Min;
        public GridVector Max;

        public MapBlockRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MapBlockRequest m = new MapBlockRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = (MapAgentFlags)p.ReadUInt32();
            m.EstateID = p.ReadUInt32();
            m.IsGodlike = p.ReadBoolean();
            m.Min.GridX = p.ReadUInt16();
            m.Max.GridX = p.ReadUInt16();
            m.Min.GridY = p.ReadUInt16();
            m.Max.GridY = p.ReadUInt16();

            return m;
        }
    }
}
