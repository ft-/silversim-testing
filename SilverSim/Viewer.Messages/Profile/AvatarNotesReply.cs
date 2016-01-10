// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarNotesReply)]
    [Reliable]
    [Trusted]
    public class AvatarNotesReply : Message
    {
        public UUID AgentID = UUID.Zero;

        public UUID TargetID = UUID.Zero;
        public string Notes;

        public AvatarNotesReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(TargetID);
            p.WriteStringLen16(Notes);
        }

        public static Message Decode(UDPPacket p)
        {
            AvatarNotesReply m = new AvatarNotesReply();
            m.AgentID = p.ReadUUID();
            m.TargetID = p.ReadUUID();
            m.Notes = p.ReadStringLen16();
            return m;
        }
    }
}
