// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Map
{
    [UDPMessage(MessageType.MapNameRequest)]
    [Reliable]
    [NotTrusted]
    public class MapNameRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public MapAgentFlags Flags;
        public UInt32 EstateID;
        public bool IsGodlike;
        public string Name;

        public MapNameRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MapNameRequest m = new MapNameRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = (MapAgentFlags)p.ReadUInt32();
            m.EstateID = p.ReadUInt32();
            m.IsGodlike = p.ReadBoolean();
            m.Name = p.ReadStringLen8();

            return m;
        }
    }
}
