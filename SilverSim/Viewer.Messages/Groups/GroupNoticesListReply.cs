// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;

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

            public int SizeInMessage => 25 + FromName.Length + Subject.ToUTF8ByteCount();
        }

        public List<GroupNoticeData> Data = new List<GroupNoticeData>();

        public override void Serialize(UDPPacket p)
        {
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
        public static Message Decode(UDPPacket p)
        {
            var m = new GroupNoticesListReply
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.Data.Add(new GroupNoticeData
                {
                    NoticeID = p.ReadUUID(),
                    Timestamp = Date.UnixTimeToDateTime(p.ReadUInt32()),
                    FromName = p.ReadStringLen16(),
                    Subject = p.ReadStringLen16(),
                    HasAttachment = p.ReadBoolean(),
                    AssetType = (AssetType)p.ReadUInt32()
                });
            }
            return m;
        }
    }
}
