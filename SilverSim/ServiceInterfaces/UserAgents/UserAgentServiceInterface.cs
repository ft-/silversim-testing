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
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.Grid;
using SilverSim.Types.ServerURIs;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.UserAgents
{
    public abstract class UserAgentServiceInterface
    {
        public struct UserInfo
        {
            public string FirstName;
            public string LastName;
            public UserFlags UserFlags;
            public Date UserCreated;
            public string UserTitle;
        }

        public abstract IDisplayNameAccessor DisplayName { get; }

        public abstract void VerifyAgent(UUID sessionID, string token);

        public abstract void VerifyClient(UUID sessionID, string token);

        public abstract List<UUID> NotifyStatus(List<KeyValuePair<UGUI, string>> friends, UGUI user, bool online);

        public abstract UserInfo GetUserInfo(UGUI user);

        public abstract ServerURIs GetServerURLs(UGUI user);

        public abstract string LocateUser(UGUI user);

        public abstract UGUIWithName GetUUI(UGUI user, UGUI targetUserID);

        public abstract DestinationInfo GetHomeRegion(UGUI user);

        public abstract void SetHomeRegion(UGUI user, UserRegionData info);

        public virtual void SetLastRegion(UGUI user, UserRegionData info)
        {
            /* intentionally left empty */
        }

        public abstract bool IsOnline(UGUI user);

        public virtual void Reauth()
        {
            /* intentionally left empty */
        }

        [Serializable]
        public class RequestFailedException : Exception
        {
            public RequestFailedException()
            {
            }

            public RequestFailedException(string message)
                : base(message)
            {
            }

            protected RequestFailedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public RequestFailedException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }
    }
}