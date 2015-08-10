// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Groups
{
    public class GroupMember
    {
        public UGI Group = UGI.Unknown;
        public UUI Principal = UUI.Unknown;
        public UUID SelectedRoleID = UUID.Zero;
        public int Contribution = 0;
        public bool IsListInProfile = false;
        public bool IsAcceptNotices = false;
        public string AccessToken = "";

        public GroupMember()
        {

        }
    }
}
