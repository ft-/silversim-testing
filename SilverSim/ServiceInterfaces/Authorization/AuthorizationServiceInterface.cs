// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.Grid;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Authorization
{
    public abstract class AuthorizationServiceInterface
    {
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
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

        public AuthorizationServiceInterface()
        {

        }

        /* throws NotAuthorizedException when not allowed */
        public abstract void Authorize(AuthorizationData ad);

        /* check access when doing QueryAccess
         * These checks are informational only to the arriving agent and 
         * cannot be granted to be validating correctly at all times.
         */
        /* throws NotAuthorizedException when not allowed */
        public abstract void QueryAccess(UUI agent, UUID regionID);
    }
}
