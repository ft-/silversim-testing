// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;

namespace SilverSim.Viewer.Messages.Groups
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
        public bool ShowInList;
        public string MemberTitle = string.Empty;
        public GroupPowers PowersMask;
        public UUID InsigniaID = UUID.Zero;
        public UUID FounderID = UUID.Zero;
        public int MembershipFee;
        public bool OpenEnrollment;
        public int Money;
        public int GroupMembershipCount;
        public int GroupRolesCount;
        public bool AllowPublish;
        public bool MaturePublish;
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
