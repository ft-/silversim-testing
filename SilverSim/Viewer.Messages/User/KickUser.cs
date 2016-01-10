// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.User
{
    [UDPMessage(MessageType.KickUser)]
    [Reliable]
    [Trusted]
    public class KickUser : Message
    {
        public UInt32 IpAddr;
        public UInt16 Port;
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public string Message = string.Empty;

        public KickUser()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt32(IpAddr);
            p.WriteUInt16(Port);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteStringLen16(Message);
        }

        public static Message Decode(UDPPacket p)
        {
            KickUser m = new KickUser();
            m.IpAddr = p.ReadUInt32();
            m.Port = p.ReadUInt16();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Message = p.ReadStringLen16();
            return m;
        }
    }
}
