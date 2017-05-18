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
using SilverSim.Types.AuthInfo;
using System;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.AuthInfo
{
    [Serializable]
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

    [Serializable]
    public class AuthenticationFailedException : Exception
    {
        public AuthenticationFailedException()
        {
        }

        public AuthenticationFailedException(string message)
             : base(message)
        {
        }

        protected AuthenticationFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
        }

        public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }

    public abstract class AuthInfoServiceInterface
    {
        public abstract UserAuthInfo this[UUID accountid] { get; }
        public abstract void Store(UserAuthInfo info);

        public abstract UUID AddToken(UUID principalId, UUID sessionid, int lifetime_in_minutes);
        public abstract void VerifyToken(UUID principalId, UUID token, int lifetime_extension_in_minutes);
        public abstract void ReleaseToken(UUID accountId, UUID secureSessionId);
        public abstract void ReleaseTokenBySession(UUID accountId, UUID sessionId);
        
        public virtual void SetPassword(UUID principalId, string password)
        {
            UserAuthInfo uai = this[principalId];
            uai.Password = password;
            Store(uai);
        }

        public virtual UUID Authenticate(UUID sessionId, UUID principalId, string password, int lifetime_in_minutes)
        {
            this[principalId].CheckPassword(password);
            return AddToken(principalId, sessionId, 30);
        }
    }
}
