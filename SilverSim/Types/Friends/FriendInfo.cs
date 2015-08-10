// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Friends
{
    public class FriendInfo
    {
        public UUI User = UUI.Unknown;
        public UUI Friend = UUI.Unknown;
        public string Secret = string.Empty;
        public int UserGivenFlags = 0;
        public int FriendGivenFlags = 0;

        public FriendInfo()
        {

        }
    }
}
