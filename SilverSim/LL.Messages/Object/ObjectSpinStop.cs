// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectSpinStop)]
    [Reliable]
    [NotTrusted]
    public class ObjectSpinStop : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID ObjectID = UUID.Zero;

        public ObjectSpinStop()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectSpinStop m = new ObjectSpinStop();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            return m;
        }
    }
}
