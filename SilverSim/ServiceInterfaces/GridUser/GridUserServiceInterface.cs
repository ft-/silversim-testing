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
using SilverSim.Types.GridUser;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.GridUser
{
    [Serializable]
    public class GridUserNotFoundException : KeyNotFoundException
    {
        public GridUserNotFoundException()
        {
        }

        public GridUserNotFoundException(string message)
            : base(message)
        {
        }

        protected GridUserNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GridUserNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class GridUserSetHomeNotPossibleForForeignerException : KeyNotFoundException
    {
        public GridUserSetHomeNotPossibleForForeignerException()
        {
        }

        public GridUserSetHomeNotPossibleForForeignerException(string message)
            : base(message)
        {
        }

        protected GridUserSetHomeNotPossibleForForeignerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GridUserSetHomeNotPossibleForForeignerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class GridUserUpdateFailedException : Exception
    {
        public GridUserUpdateFailedException()
        {
        }

        public GridUserUpdateFailedException(string message)
            : base(message)
        {
        }

        protected GridUserUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GridUserUpdateFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public abstract class GridUserServiceInterface
    {
        public GridUserInfo this[UUID userID]
        {
            get
            {
                GridUserInfo info;
                if(!TryGetValue(userID, out info))
                {
                    throw new GridUserNotFoundException();
                }
                return info;
            }
        }

        public abstract bool TryGetValue(UUID userID, out GridUserInfo userInfo);

        public GridUserInfo this[UGUI userID]
        {
            get
            {
                GridUserInfo info;
                if(!TryGetValue(userID, out info))
                {
                    throw new GridUserNotFoundException();
                }
                return info;
            }
        }

        public abstract bool TryGetValue(UGUI userID, out GridUserInfo userInfo);

        public abstract void LoggedInAdd(UGUI userID); /* LoggedInAdd is only supported by DB services */
        public abstract void LoggedIn(UGUI userID);
        public abstract void LoggedOut(UGUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt);
        public virtual void LoggedOut(UGUI userID)
        {
            /* intentionally left empty */
        }
        public abstract void SetHome(UGUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt);
        public abstract void SetPosition(UGUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt);
    }
}
