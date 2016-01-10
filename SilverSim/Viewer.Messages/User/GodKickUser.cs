// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.User
{
    [UDPMessage(MessageType.GodKickUser)]
    [Reliable]
    [NotTrusted]
    public class GodKickUser : Message
    {
        public UUID GodID = UUID.Zero;
        public UUID GodSessionID = UUID.Zero;
        public UUID AgentID = UUID.Zero;
        public UInt32 KickFlags;
        public string Reason;

        public GodKickUser()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            GodKickUser m = new GodKickUser();
            m.GodID = p.ReadUUID();
            m.GodSessionID = p.ReadUUID();
            m.AgentID = p.ReadUUID();
            m.KickFlags = p.ReadUInt32();
            m.Reason = p.ReadStringLen16();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(GodID);
            p.WriteUUID(GodSessionID);
            p.WriteUUID(AgentID);
            p.WriteUInt32(KickFlags);
            p.WriteStringLen16(Reason);
        }
    }
}
