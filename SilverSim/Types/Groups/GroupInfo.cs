// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Groups
{
    public class DirGroupInfo
    {
        public UGI ID = UGI.Unknown;
        public int MemberCount = 0;
        public float SearchOrder = 0;

        public DirGroupInfo()
        {

        }
    }

    public class GroupInfo
    {
        public UGI ID = UGI.Unknown;
        public string Charter = "";
        public string Location = "";
        public UUID InsigniaID = UUID.Zero;
        public UUI Founder = UUI.Unknown;
        public int MembershipFee = 0;
        public bool IsOpenEnrollment = false;
        public bool IsShownInList = false;
        public bool IsAllowPublish = false;
        public bool IsMaturePublish = false;
        public UUID OwnerRoleID = UUID.Zero;

        #region Informational fields
        public int MemberCount = 0;
        public int RoleCount = 0;
        #endregion

        public GroupInfo()
        {

        }
    }
}
