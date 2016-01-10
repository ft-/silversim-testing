// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Region
{
    [UDPMessage(MessageType.RegionHandshakeReply)]
    [Reliable]
    [NotTrusted]
    public class RegionHandshakeReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 Flags;

        public RegionHandshakeReply()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RegionHandshakeReply m = new RegionHandshakeReply();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = p.ReadUInt32();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(Flags);
        }
    }
}
