// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.GridUser
{
    public class GridUserInfo
    {
        public GridUserInfo()
        {

        }

        public UUI User = UUI.Unknown;
        public UUID HomeRegionID = UUID.Zero;
        public Vector3 HomePosition = Vector3.Zero;
        public Vector3 HomeLookAt = Vector3.Zero;
        public UUID LastRegionID = UUID.Zero;
        public Vector3 LastPosition = Vector3.Zero;
        public Vector3 LastLookAt = Vector3.Zero;
        public bool IsOnline;
        public Date LastLogin = new Date();
        public Date LastLogout = new Date();
    }
}
