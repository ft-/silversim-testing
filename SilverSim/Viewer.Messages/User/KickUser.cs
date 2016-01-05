// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.User
{
    [UDPMessage(MessageType.KickUser)]
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

        public override bool IsReliable
        {
            get
            {
                return true;
            }
        }

        public override MessageType Number
        {
            get
            {
                return MessageType.KickUser;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(MessageType.KickUser);
            p.WriteUInt32(IpAddr);
            p.WriteUInt16(Port);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteStringLen16(Message);
        }
    }
}
