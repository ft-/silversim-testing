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
        public int IntervalDays = 0;
        public int CurrentInterval = 0;
        public string StartDate = string.Empty;
        public string Description = string.Empty;
        public int Amount = 0;

        public GroupAccountDetailsReply()
        {

        }

        public override void Serialize(UDPPacket p)
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
        }
    }
}
