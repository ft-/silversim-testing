// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.SetGroupContribution)]
    [Reliable]
    [NotTrusted]
    public class SetGroupContribution : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public int Contribution;

        public SetGroupContribution()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            SetGroupContribution m = new SetGroupContribution();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Contribution = p.ReadInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteInt32(Contribution);
        }
    }
}
