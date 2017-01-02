// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Database.MySQL.Groups
{
    public static class MySQLGroupsServiceExtensionMethods
    {
        public static GroupInfo ToGroupInfo(this MySqlDataReader reader, string memberCount = "MemberCount")
        {
            GroupInfo info = new GroupInfo();
            info.ID.ID = reader.GetUUID("GroupID");
            string uri = reader.GetString("Location");
            if (!string.IsNullOrEmpty(uri))
            {
                info.ID.HomeURI = new Uri(uri, UriKind.Absolute);
            }
            info.ID.GroupName = reader.GetString("Name");
            info.Charter = reader.GetString("Charter");
            info.InsigniaID = reader.GetUUID("InsigniaID");
            info.Founder = reader.GetUUI("Founder");
            info.MembershipFee = reader.GetInt32("MembershipFee");
            info.IsOpenEnrollment = reader.GetBool("OpenEnrollment");
            info.IsShownInList = reader.GetBool("ShownInList");
            info.IsAllowPublish = reader.GetBool("AllowPublish");
            info.IsMaturePublish = reader.GetBool("MaturePublish");
            info.OwnerRoleID = reader.GetUUID("OwnerRoleID");
            info.MemberCount = reader.GetInt32(memberCount);
            info.RoleCount = reader.GetInt32("RoleCount");

            return info;
        }

        public static GroupRole ToGroupRole(this MySqlDataReader reader, string prefix = "")
        {
            GroupRole role = new GroupRole();
            role.Group.ID = reader.GetUUID("GroupID");
            role.ID = reader.GetUUID("RoleID");
            role.Name = reader.GetString(prefix + "Name");
            role.Description = reader.GetString(prefix + "Description");
            role.Title = reader.GetString(prefix + "Title");
            role.Powers = reader.GetEnum<GroupPowers>(prefix + "Powers");
            if(role.ID == UUID.Zero)
            {
                role.Members = reader.GetUInt32("GroupMembers");
            }

            return role;
        }

        public static GroupMember ToGroupMember(this MySqlDataReader reader)
        {
            GroupMember groupmem = new GroupMember();
            groupmem.Group.ID = reader.GetUUID("GroupID");
            groupmem.Principal = reader.GetUUI("Principal");
            groupmem.SelectedRoleID = reader.GetUUID("SelectedRoleID");
            groupmem.Contribution = reader.GetInt32("Contribution");
            groupmem.IsListInProfile = reader.GetBool("ListInProfile");
            groupmem.IsAcceptNotices = reader.GetBool("AcceptNotices");
            groupmem.AccessToken = reader.GetString("AccessToken");

            return groupmem;
        }

        public static GroupRolemember ToGroupRolemember(this MySqlDataReader reader)
        {
            GroupRolemember grouprolemem = new GroupRolemember();
            grouprolemem.Group.ID = reader.GetUUID("GroupID");
            grouprolemem.RoleID = reader.GetUUID("RoleID");
            grouprolemem.Principal = reader.GetUUI("Principal");
            grouprolemem.Powers = reader.GetEnum<GroupPowers>("Powers");

            return grouprolemem;
        }

        public static GroupRolemember ToGroupRolememberEveryone(this MySqlDataReader reader, GroupPowers powers)
        {
            GroupRolemember grouprolemem = new GroupRolemember();
            grouprolemem.Group.ID = reader.GetUUID("GroupID");
            grouprolemem.RoleID = UUID.Zero;
            grouprolemem.Principal = reader.GetUUI("Principal");
            grouprolemem.Powers = powers;

            return grouprolemem;
        }

        public static GroupRolemembership ToGroupRolemembership(this MySqlDataReader reader)
        {
            GroupRolemembership grouprolemem = new GroupRolemembership();
            grouprolemem.Group.ID = reader.GetUUID("GroupID");
            grouprolemem.RoleID = reader.GetUUID("RoleID");
            grouprolemem.Principal = reader.GetUUI("Principal");
            grouprolemem.Powers = reader.GetEnum<GroupPowers>("Powers");
            grouprolemem.GroupTitle = reader.GetString("Title");

            return grouprolemem;
        }

        public static GroupRolemembership ToGroupRolemembershipEveryone(this MySqlDataReader reader, GroupPowers powers)
        {
            GroupRolemembership grouprolemem = new GroupRolemembership();
            grouprolemem.Group.ID = reader.GetUUID("GroupID");
            grouprolemem.RoleID = UUID.Zero;
            grouprolemem.Principal = reader.GetUUI("Principal");
            grouprolemem.Powers = powers;

            return grouprolemem;
        }

        public static GroupInvite ToGroupInvite(this MySqlDataReader reader)
        {
            GroupInvite inv = new GroupInvite();
            inv.ID = reader.GetUUID("InviteID");
            inv.Group.ID = reader.GetUUID("GroupID");
            inv.RoleID = reader.GetUUID("RoleID");
            inv.Principal = reader.GetUUI("Principal");
            inv.Timestamp = reader.GetDate("Timestamp");

            return inv;
        }

        public static GroupNotice ToGroupNotice(this MySqlDataReader reader)
        {
            GroupNotice notice = new GroupNotice();
            notice.Group.ID = reader.GetUUID("GroupID");
            notice.ID = reader.GetUUID("NoticeID");
            notice.Timestamp = reader.GetDate("Timestamp");
            notice.FromName = reader.GetString("FromName");
            notice.Subject = reader.GetString("Subject");
            notice.Message = reader.GetString("Message");
            notice.HasAttachment = reader.GetBool("HasAttachment");
            notice.AttachmentType = reader.GetEnum<AssetType>("AttachmentType");
            notice.AttachmentName = reader.GetString("AttachmentName");
            notice.AttachmentItemID = reader.GetUUID("AttachmentItemID");
            notice.AttachmentOwner.ID = reader.GetUUID("AttachmentOwnerID");

            return notice;
        }
    }
}
