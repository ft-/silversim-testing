/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.Profile
{
    public class AvatarGroupsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID AvatarID = UUID.Zero;

        public struct GroupDataEntry
        {
            public UInt64 GroupPowers;
            public bool AcceptNotices;
            public string GroupTitle;
            public UUID GroupID;
            public string GroupName;
            public UUID GroupInsigniaID;
        }

        public List<GroupDataEntry> GroupData = new List<GroupDataEntry>();
        public bool ListInProfile = false;

        public AvatarGroupsReply()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.AvatarGroupsReply;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(AvatarID);
            p.WriteUInt8((byte)GroupData.Count);
            foreach(GroupDataEntry d in GroupData)
            {
                p.WriteUInt64(d.GroupPowers);
                p.WriteBoolean(d.AcceptNotices);
                p.WriteStringLen8(d.GroupTitle);
                p.WriteUUID(d.GroupID);
                p.WriteStringLen8(d.GroupName);
                p.WriteUUID(d.GroupInsigniaID);
            }
            p.WriteBoolean(ListInProfile);
        }
    }
}
