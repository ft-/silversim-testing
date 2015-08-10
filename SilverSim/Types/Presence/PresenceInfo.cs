// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Presence
{
    public class PresenceInfo
    {
        public UUI UserID = UUI.Unknown;
        public UUID RegionID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID SecureSessionID = UUID.Zero;

        public PresenceInfo()
        {

        }
    }
}
