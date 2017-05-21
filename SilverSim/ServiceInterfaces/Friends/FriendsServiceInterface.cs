// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract FriendInfo this[UUI user, UUI friend] { get; }

        public abstract List<FriendInfo> this[UUI user] { get; }

        public abstract bool TryGetValue(UUI user, UUI friend, out FriendInfo fInfo);

        public abstract void Store(FriendInfo fi);

        public abstract void StoreRights(FriendInfo fi);

        public abstract void StoreOffer(FriendInfo fi);

        public abstract void Delete(FriendInfo fi);
    }
}
