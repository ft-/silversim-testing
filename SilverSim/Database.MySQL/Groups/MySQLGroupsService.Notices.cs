// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupNoticesInterface
    {
        GroupNotice IGroupNoticesInterface.this[UUI requestingAgent, UUID groupNoticeID]
        {
            get
            {
                GroupNotice notice = new GroupNotice();
                if(!Notices.TryGetValue(requestingAgent, groupNoticeID, out notice))
                {
                    throw new KeyNotFoundException();
                }
                return notice;
            }
        }

        void IGroupNoticesInterface.Add(UUI requestingAgent, GroupNotice notice)
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("GroupID", notice.Group.ID);
            vals.Add("NoticeID", notice.ID);
            vals.Add("Timestamp", notice.Timestamp);
            vals.Add("FromName", notice.Timestamp);
            vals.Add("Subject", notice.Subject);
            vals.Add("Message", notice.Message);
            vals.Add("HasAttachment", notice.HasAttachment);
            vals.Add("AttachmentType", notice.AttachmentType);
            vals.Add("AttachmentName", notice.AttachmentName);
            vals.Add("AttachmentItemID", notice.AttachmentItemID);
            vals.Add("AttachmentOwnerID", notice.AttachmentOwner.ID);

            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("groupnotices", vals);
            }
        }

        void IGroupNoticesInterface.Delete(UUI requestingAgent, UUID groupNoticeID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM groupinvites WHERE InviteID LIKE ?inviteid", conn))
                {
                    cmd.Parameters.AddParameter("?inviteid", groupNoticeID);
                    if(cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        List<GroupNotice> IGroupNoticesInterface.GetNotices(UUI requestingAgent, UGI group)
        {
            List<GroupNotice> notices = new List<GroupNotice>();
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupnotices WHERE GroupID LIKE ?groupid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            GroupNotice notice = reader.ToGroupNotice();
                            notice.Group = ResolveName(requestingAgent, notice.Group);
                            notices.Add(notice);
                        }
                    }
                }
            }

            return notices;
        }

        bool IGroupNoticesInterface.TryGetValue(UUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice)
        {
            GroupNotice notice;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupnotices WHERE NoticeID LIKE ?noticeid", conn))
                {
                    cmd.Parameters.AddParameter("?noticeid", groupNoticeID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(!reader.Read())
                        {
                            groupNotice = null;
                            return false;
                        }
                        notice = reader.ToGroupNotice();
                        notice.Group = ResolveName(requestingAgent, notice.Group);
                    }
                }
            }

            groupNotice = notice;
            return true;
        }

        bool IGroupNoticesInterface.ContainsKey(UUI requestingAgent, UUID groupNoticeID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT NoticeID FROM groupnotices WHERE NoticeID LIKE ?noticeid", conn))
                {
                    cmd.Parameters.AddParameter("?noticeid", groupNoticeID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
    }
}
