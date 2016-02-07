// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Friends;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Friends
{
    public abstract class FriendsServiceInterface
    {
        [Serializable]
        public class FriendUpdateFailedException : Exception
        {
            public FriendUpdateFailedException()
            {

            }

            public FriendUpdateFailedException(string message)
                : base(message)
            {

            }

            protected FriendUpdateFailedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public FriendUpdateFailedException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        public FriendsServiceInterface()
        {

        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract FriendInfo this[UUI user, UUI friend]
        {
            get;
        }

        public abstract List<FriendInfo> this[UUI user]
        {
            get;
        }

        public abstract bool TryGetValue(UUI user, UUI friend, out FriendInfo fInfo);

        public abstract void Store(FriendInfo fi);

        public abstract void StoreRights(FriendInfo fi);

        public abstract void StoreOffer(FriendInfo fi);

        public abstract void Delete(FriendInfo fi);
    }
}
