// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupNoticesListReply)]
    [Reliable]
    [Trusted]
    public class GroupNoticesListReply : Message
    {
        public UUID AgentID;
        public UUID GroupID;

        public struct GroupNoticeData
        {
            public UUID NoticeID;
            public Date Timestamp;
            public string FromName;
            public string Subject;
            public bool HasAttachment;
            public AssetType AssetType;

            public int SizeInMessage
            {
                get
                {
                    return 25 + FromName.Length + Subject.ToUTF8ByteCount();
                }
            }
        }

        public List<GroupNoticeData> Data = new List<GroupNoticeData>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);

            p.WriteUInt8((byte)Data.Count);
            foreach(GroupNoticeData d in Data)
            {
                p.WriteUUID(d.NoticeID);
                p.WriteUInt32((uint)d.Timestamp.DateTimeToUnixTime());
                p.WriteStringLen16(d.FromName);
                p.WriteStringLen16(d.Subject);
                p.WriteBoolean(d.HasAttachment);
                p.WriteUInt8((byte)d.AssetType);
            }
        }
    }
}
