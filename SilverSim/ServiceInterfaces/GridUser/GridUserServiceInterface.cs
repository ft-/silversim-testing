// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.GridUser;
using System.Collections.Generic;
using System;
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
        public GridUserServiceInterface()
        {

        }

        public abstract GridUserInfo this[UUID userID]
        {
            get;
        }

        public abstract bool TryGetValue(UUID userID, out GridUserInfo userInfo);

        public abstract GridUserInfo this[UUI userID]
        {
            get;
        }

        public abstract bool TryGetValue(UUI userID, out GridUserInfo userInfo);

        public abstract void LoggedInAdd(UUI userID); /* LoggedInAdd is only supported by DB services */
        public abstract void LoggedIn(UUI userID);
        public abstract void LoggedOut(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt);
        public abstract void SetHome(UUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt);
        public abstract void SetPosition(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt);
    }
}
