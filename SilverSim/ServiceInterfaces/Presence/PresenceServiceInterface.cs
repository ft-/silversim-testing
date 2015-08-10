// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Presence
{
    public class PresenceUpdateFailedException : Exception
    {
        public PresenceUpdateFailedException()
        {

        }
    }

    public class PresenceNotFoundException : Exception
    {
        public PresenceNotFoundException()
        {

        }
    }

    public class PresenceLogoutRegionFailedException : Exception
    {
        public PresenceLogoutRegionFailedException()
        {
        }
    }

    public abstract class PresenceServiceInterface
    {
        public enum SetType
        {
            Login,
            Report
        }

        public PresenceServiceInterface()
        {

        }

        public abstract List<PresenceInfo> this[UUID userID]
        {
            get;
        }

        public abstract PresenceInfo this[UUID sessionID, UUID userID]
        {
            get;
            set; /* setting null means logout, != null not allowed */
        }

        public abstract PresenceInfo this[UUID sessionID, UUID userID, SetType reportType]
        {
            set; /* setting null means logout, != null login message */
        }

        public abstract void logoutRegion(UUID regionID);
    }
}
