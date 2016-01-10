// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Map
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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32((uint)Flags);
            p.WriteUInt32(EstateID);
            p.WriteBoolean(IsGodlike);
            p.WriteStringLen8(Name);
        }
    }
}
