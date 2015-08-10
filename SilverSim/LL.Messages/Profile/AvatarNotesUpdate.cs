// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.AvatarNotesUpdate)]
    [Reliable]
    [NotTrusted]
    public class AvatarNotesUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID TargetID = UUID.Zero;
        public string Notes;

        public AvatarNotesUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AvatarNotesUpdate m = new AvatarNotesUpdate();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TargetID = p.ReadUUID();
            m.Notes = p.ReadStringLen16();

            return m;
        }
    }
}
