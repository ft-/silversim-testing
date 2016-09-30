// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Account;
using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Presence
{
    [Serializable]
    public sealed class PresenceUpdateFailedException : Exception
    {
        public PresenceUpdateFailedException()
        {

        }

        public PresenceUpdateFailedException(string message)
            : base(message)
        {

        }

        PresenceUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public PresenceUpdateFailedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public sealed class PresenceNotFoundException : Exception
    {
        public PresenceNotFoundException()
        {

        }

        public PresenceNotFoundException(string message)
            : base(message)
        {

        }

        PresenceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public PresenceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public sealed class PresenceLogoutRegionFailedException : Exception
    {
        public PresenceLogoutRegionFailedException()
        {
        }

        public PresenceLogoutRegionFailedException(string message)
            : base(message)
        {

        }

        PresenceLogoutRegionFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public PresenceLogoutRegionFailedException(string message, Exception innerException)
            : base(message, innerException)
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

        public abstract List<PresenceInfo> GetPresencesInRegion(UUID regionId);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract PresenceInfo this[UUID sessionID, UUID userID]
        {
            get;
            set; /* setting null means logout, != null not allowed */
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public abstract PresenceInfo this[UUID sessionID, UUID userID, SetType reportType]
        {
            set; /* setting null means logout, != null login message */
        }

        public abstract void LogoutRegion(UUID regionID);
        public abstract void Remove(UUID scopeID, UUID accountID);
    }
}
