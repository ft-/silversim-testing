// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectSpinUpdate)]
    [Reliable]
    [NotTrusted]
    public class ObjectSpinUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID ObjectID = UUID.Zero;
        public Quaternion Rotation = Quaternion.Identity;

        public ObjectSpinUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectSpinUpdate m = new ObjectSpinUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.Rotation = p.ReadLLQuaternion();
            return m;
        }
    }
}
