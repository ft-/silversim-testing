using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types.Friends;
using ArribaSim.Types;

namespace ArribaSim.ServiceInterfaces.Friends
{
    public abstract class FriendsServiceInterface
    {
        public FriendsServiceInterface()
        {

        }

        public abstract FriendInfo this[UUI user, UUI friend]
        {
            get;
        }

        public abstract FriendInfo this[UUID userID, UUI friendID]
        {
            get;
        }

        public abstract List<FriendInfo> this[UUID userID]
        {
            get;
        }
    }
}
