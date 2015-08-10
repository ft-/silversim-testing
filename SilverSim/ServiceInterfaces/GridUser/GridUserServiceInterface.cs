// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.GridUser;
using System.Collections.Generic;
using System;

namespace SilverSim.ServiceInterfaces.GridUser
{
    public class GridUserNotFoundException : Exception
    {
        public GridUserNotFoundException()
        {

        }
    }
    public class GridUserUpdateFailedException : Exception
    {
        public GridUserUpdateFailedException()
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
        public abstract GridUserInfo this[UUI userID]
        {
            get;
        }

        public abstract void LoggedInAdd(UUI userID); /* LoggedInAdd is only supported by DB services */
        public abstract void LoggedIn(UUI userID);
        public abstract void LoggedOut(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt);
        public abstract void SetHome(UUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt);
        public abstract void SetPosition(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt);
    }
}
