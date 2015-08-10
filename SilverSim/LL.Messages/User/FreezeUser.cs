// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.User
{
    [UDPMessage(MessageType.FreezeUser)]
    [Reliable]
    [NotTrusted]
    public class FreezeUser : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID TargetID = UUID.Zero;
        public UInt32 Flags;

        public FreezeUser()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            EjectUser m = new EjectUser();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TargetID = p.ReadUUID();
            m.Flags = p.ReadUInt32();

            return m;
        }
    }
}
