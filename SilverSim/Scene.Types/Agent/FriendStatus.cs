// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Friends;

namespace SilverSim.Scene.Types.Agent
{
    public class FriendStatus : FriendInfo
    {
        public bool IsOnline;

        public FriendStatus()
        {

        }

        public FriendStatus(FriendInfo fi)
        {
            User = fi.User;
            Friend = fi.Friend;
            Secret = fi.Secret;
            UserGivenFlags = fi.UserGivenFlags;
            FriendGivenFlags = fi.FriendGivenFlags;
        }
    }
}
