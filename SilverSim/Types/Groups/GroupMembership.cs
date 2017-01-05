// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Groups
{
    public class GroupMembership
    {
        public UGI Group = UGI.Unknown;
        public UUI Principal = UUI.Unknown;
        public GroupPowers GroupPowers = GroupPowers.None;
        public bool IsAcceptNotices;
        public UUID GroupInsigniaID = UUID.Zero;
        public int Contribution;
        public string GroupTitle = string.Empty;
        public bool IsListInProfile;
        public bool IsAllowPublish;
        public string Charter;
        public UUID ActiveRoleID;
        public UUI Founder = UUI.Unknown;
        public string AccessToken;
        public bool IsMaturePublish;
        public bool IsOpenEnrollment;
        public int MembershipFee;
        public bool IsShownInList;

        public GroupMembership()
        {

        }
    }
}
