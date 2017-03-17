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
            /* If userID is set to UUID.Zero, the session has to be retrieved/ deleted based on sessionID alone */
            get;
            set; /* setting null means logout, != null not allowed */
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public abstract PresenceInfo this[UUID sessionID, UUID userID, SetType reportType]
        {
            /* If userID is set to UUID.Zero, the session has to be retrieved/ deleted based on sessionID alone */
            set; /* setting null means logout, != null login message */
        }

        public abstract void LogoutRegion(UUID regionID);
        public abstract void Remove(UUID scopeID, UUID accountID);
    }
}
