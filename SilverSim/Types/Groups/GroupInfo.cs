// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Groups
{
    public class DirGroupInfo
    {
        public UGI ID = UGI.Unknown;
        public int MemberCount;
        public float SearchOrder;

        public DirGroupInfo()
        {

        }
    }

    public class GroupInfo
    {
        public UGI ID = UGI.Unknown;
        public string Charter = string.Empty;
        public string Location = string.Empty;
        public UUID InsigniaID = UUID.Zero;
        public UUI Founder = UUI.Unknown;
        public int MembershipFee;
        public bool IsOpenEnrollment;
        public bool IsShownInList;
        public bool IsAllowPublish;
        public bool IsMaturePublish;
        public UUID OwnerRoleID = UUID.Zero;

        #region Informational fields
        public int MemberCount;
        public int RoleCount;
        #endregion

        public GroupInfo()
        {

        }
    }
}
