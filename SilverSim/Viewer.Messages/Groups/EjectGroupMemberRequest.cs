// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [Zerocoded]
    [Reliable]
    [UDPMessage(MessageType.EjectGroupMemberRequest)]
    [NotTrusted]
    public class EjectGroupMemberRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID EjecteeID = UUID.Zero;

        public EjectGroupMemberRequest()
        {

        }

        public override MessageType Number
        {
            get 
            {
                return MessageType.EjectGroupMemberRequest;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            EjectGroupMemberRequest m = new EjectGroupMemberRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.EjecteeID = p.ReadUUID();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUUID(EjecteeID);
        }
    }
}
