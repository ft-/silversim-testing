// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupTitlesReply)]
    [Reliable]
    [Trusted]
    public class GroupTitlesReply : Message
    {
        public UUID AgentID;
        public UUID GroupID;
        public UUID RequestID;

        public struct GroupDataEntry
        {
            public string Title;
            public UUID RoleID;
            public bool Selected;
            public int SizeInMessage
            {
                get
                {
                    return 20 + Title.ToUTF8ByteCount();
                }
            }
        }

        public List<GroupDataEntry> GroupData = new List<GroupDataEntry>();

        public GroupTitlesReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteUInt8((byte)GroupData.Count);
            foreach(GroupDataEntry e in GroupData)
            {
                p.WriteStringLen8(e.Title);
                p.WriteUUID(e.RoleID);
                p.WriteBoolean(e.Selected);
            }
        }
    }
}
