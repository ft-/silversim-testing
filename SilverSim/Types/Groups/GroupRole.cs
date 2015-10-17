// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Groups
{
    public class GroupRole
    {
        public UGI Group = UGI.Unknown;
        public UUID ID = UUID.Zero;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string Title = string.Empty;
        public GroupPowers Powers;

        #region Informational fields
        public uint Members;
        #endregion

        public GroupRole()
        {

        }
    }
}
