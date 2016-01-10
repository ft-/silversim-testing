// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.IM;

namespace SilverSim.Viewer.Messages.Groups
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
            p.WriteUUID(AgentID);
            p.WriteUUID(ToGroupID);
            p.WriteUUID(ID);
            p.WriteUInt8((byte)Dialog);
            p.WriteStringLen8(FromAgentName);
            p.WriteStringLen16(Message);
            p.WriteUInt16((ushort)BinaryBucket.Length);
            p.WriteBytes(BinaryBucket);
        }

        public static Message Decode(UDPPacket p)
        {
            GroupNoticeAdd m = new GroupNoticeAdd();
            m.AgentID = p.ReadUUID();
            m.ToGroupID = p.ReadUUID();
            m.ID = p.ReadUUID();
            m.Dialog = (GridInstantMessageDialog)p.ReadUInt8();
            m.FromAgentName = p.ReadStringLen8();
            m.Message = p.ReadStringLen16();
            m.BinaryBucket = p.ReadBytes(p.ReadUInt16());
            return m;
        }
    }
}
