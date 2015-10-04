// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Map
{
    [UDPMessage(MessageType.MapLayerRequest)]
    [Reliable]
    [NotTrusted]
    public class MapLayerRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public MapAgentFlags Flags;
        public UInt32 EstateID;
        public bool IsGodlike;

        public MapLayerRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MapLayerRequest m = new MapLayerRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = (MapAgentFlags)p.ReadUInt32();
            m.EstateID = p.ReadUInt32();
            m.IsGodlike = p.ReadBoolean();

            return m;
        }
    }
}
