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
using System;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Authorization
{
    public abstract class AuthorizationServiceInterface
    {
        public struct AuthorizationData
        {
            public DestinationInfo DestinationInfo;
            public ClientInfo ClientInfo;
            public SessionInfo SessionInfo;
            public UserAccount AccountInfo;
            public AppearanceInfo AppearanceInfo;
        }

        [Serializable]
        public class NotAuthorizedException : Exception
        {
            public NotAuthorizedException()
            {
            }

            public NotAuthorizedException(string message)
                : base(message)
            {
            }

            protected NotAuthorizedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public NotAuthorizedException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        /* throws NotAuthorizedException when not allowed */
        public abstract void Authorize(AuthorizationData ad);

        /* check access when doing QueryAccess
         * These checks are informational only to the arriving agent and 
         * cannot be granted to be validating correctly at all times.
         */
        /* throws NotAuthorizedException when not allowed */
        public abstract void QueryAccess(UGUI agent, UUID regionID);
    }
}
