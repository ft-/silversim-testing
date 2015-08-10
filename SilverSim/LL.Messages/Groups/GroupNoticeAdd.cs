// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.IM;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupNoticeAdd)]
    [Reliable]
    [Trusted]
    public class GroupNoticeAdd : Message
    {
        public UUID AgentID;

        public UUID ToGroupID;
        public UUID ID;
        public GridInstantMessageDialog Dialog;
        public string FromAgentName;
        public string Message;
        public byte[] BinaryBucket = new byte[0];

        public GroupNoticeAdd()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(ToGroupID);
            p.WriteUUID(ID);
            p.WriteUInt8((byte)Dialog);
            p.WriteStringLen8(FromAgentName);
            p.WriteStringLen16(Message);
            p.WriteUInt16((ushort)BinaryBucket.Length);
            p.WriteBytes(BinaryBucket);
        }
    }
}
