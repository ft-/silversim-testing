// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectFlagUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ObjectFlagUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 ObjectLocalID = 0;
        public bool UsePhysics = false;
        public bool IsTemporary = false;
        public bool IsPhantom = false;
        public bool CastsShadows = false;

        public ObjectFlagUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectFlagUpdate m = new ObjectFlagUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectLocalID = p.ReadUInt32();
            m.UsePhysics = p.ReadBoolean();
            m.IsTemporary = p.ReadBoolean();
            m.IsPhantom = p.ReadBoolean();
            m.CastsShadows = p.ReadBoolean();
            return m;
        }
    }
}
