// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupAccountDetailsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class GroupAccountDetailsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public int IntervalDays;
        public int CurrentInterval;
        public string StartDate = string.Empty;
        public string Description = string.Empty;
        public int Amount;

        public GroupAccountDetailsReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteInt32(IntervalDays);
            p.WriteInt32(CurrentInterval);
            p.WriteStringLen8(StartDate);
            p.WriteStringLen8(Description);
            p.WriteInt32(Amount);
        }

        public static Message Decode(UDPPacket p)
        {
            GroupAccountDetailsReply m = new GroupAccountDetailsReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.IntervalDays = p.ReadInt32();
            m.CurrentInterval = p.ReadInt32();
            m.StartDate = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();
            m.Amount = p.ReadInt32();
            return m;
        }
    }
}
