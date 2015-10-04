// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Map
{
    [UDPMessage(MessageType.MapItemRequest)]
    [Reliable]
    [NotTrusted]
    public class MapItemRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public MapAgentFlags Flags;
        public UInt32 EstateID;
        public bool IsGodlike;

        public MapItemType ItemType;
        public GridVector Location;

        public MapItemRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MapItemRequest m = new MapItemRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = (MapAgentFlags)p.ReadUInt32();
            m.EstateID = p.ReadUInt32();
            m.IsGodlike = p.ReadBoolean();
            m.ItemType = (MapItemType)p.ReadUInt32();
            m.Location.RegionHandle = p.ReadUInt64();

            return m;
        }
    }
}
