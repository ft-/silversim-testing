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

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupProfileReply)]
    [Reliable]
    [Trusted]
    public class GroupProfileReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public string Name = string.Empty;
        public string Charter = string.Empty;
        public bool ShowInList = false;
        public string MemberTitle = string.Empty;
        public GroupPowers PowersMask = GroupPowers.None;
        public UUID InsigniaID = UUID.Zero;
        public UUID FounderID = UUID.Zero;
        public int MembershipFee = 0;
        public bool OpenEnrollment = false;
        public int Money = 0;
        public int GroupMembershipCount = 0;
        public int GroupRolesCount = 0;
        public bool AllowPublish = false;
        public bool MaturePublish = false;
        public UUID OwnerRoleID = UUID.Zero;

        public GroupProfileReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteStringLen8(Name);
            p.WriteStringLen16(Charter);
            p.WriteBoolean(ShowInList);
            p.WriteStringLen8(MemberTitle);
            p.WriteUInt64((UInt64)PowersMask);
            p.WriteUUID(InsigniaID);
            p.WriteUUID(FounderID);
            p.WriteInt32(MembershipFee);
            p.WriteBoolean(OpenEnrollment);
            p.WriteInt32(Money);
            p.WriteInt32(GroupMembershipCount);
            p.WriteInt32(GroupRolesCount);
            p.WriteBoolean(AllowPublish);
            p.WriteBoolean(MaturePublish);
            p.WriteUUID(OwnerRoleID);
        }
    }
}
