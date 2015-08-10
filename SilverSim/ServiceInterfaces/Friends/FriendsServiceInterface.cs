// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Friends;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Friends
{
    public abstract class FriendsServiceInterface
    {
        public class FriendUpdateFailedException : Exception
        {

        }

        public FriendsServiceInterface()
        {

        }

        public abstract FriendInfo this[UUI user, UUI friend]
        {
            get;
        }

        public abstract List<FriendInfo> this[UUI user]
        {
            get;
        }

        public abstract void Store(FriendInfo fi);

        public abstract void Delete(FriendInfo fi);
    }
}
