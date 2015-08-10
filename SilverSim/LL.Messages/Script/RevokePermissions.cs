// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Script
{
    [UDPMessage(MessageType.RevokePermissions)]
    [Reliable]
    [NotTrusted]
    public class RevokePermissions : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID ObjectID = UUID.Zero;
        public UInt32 ObjectPermissions;

        public RevokePermissions()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RevokePermissions m = new RevokePermissions();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.ObjectPermissions = p.ReadUInt32();

            return m;
        }
    }
}
