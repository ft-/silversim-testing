// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarPropertiesRequest)]
    [Reliable]
    [NotTrusted]
    public class AvatarPropertiesRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID AvatarID = UUID.Zero;

        public AvatarPropertiesRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AvatarPropertiesRequest m = new AvatarPropertiesRequest();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.AvatarID = p.ReadUUID();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(AvatarID);
        }
    }
}
