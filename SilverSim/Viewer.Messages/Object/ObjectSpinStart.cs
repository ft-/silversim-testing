// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectSpinStart)]
    [Reliable]
    [NotTrusted]
    public class ObjectSpinStart : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID ObjectID = UUID.Zero;

        public ObjectSpinStart()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectSpinStart m = new ObjectSpinStart();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ObjectID);
        }
    }
}
