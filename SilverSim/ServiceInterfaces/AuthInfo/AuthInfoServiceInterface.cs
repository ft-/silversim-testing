// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.AuthInfo;
using System;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.AuthInfo
{
    public class VerifyTokenFailedException : Exception
    {
        public VerifyTokenFailedException()
        {

        }

        public VerifyTokenFailedException(string message)
             : base(message)
        {

        }

        protected VerifyTokenFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {

        }

        public VerifyTokenFailedException(string message, Exception innerException)
        : base(message, innerException)
        {

        }
    }

    public abstract class AuthInfoServiceInterface
    {
        protected AuthInfoServiceInterface()
        {

        }

        public abstract UserAuthInfo this[UUID accountid] { get; }
        public abstract void Store(UserAuthInfo info);

        public abstract UUID AddToken(UUID principalId, int lifetime_in_minutes);
        public abstract void VerifyToken(UUID principalId, UUID token, int lifetime_extension_in_minutes);
        public abstract void ReleaseToken(UUID accountId, UUID secureSessionId);
    }
}
