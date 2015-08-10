// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Groups
{
    public class GroupRolemember
    {
        public UGI Group = UGI.Unknown;
        public UUID RoleID = UUID.Zero;
        public UUI Principal = UUI.Unknown;

        public GroupRolemember()
        {

        }
    }
}
