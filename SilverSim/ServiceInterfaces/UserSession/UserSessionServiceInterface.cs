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
using SilverSim.Types.UserSession;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.UserSession
{
    public abstract class UserSessionServiceInterface : IPresenceServiceInterface
    {
        public abstract UserSessionInfo CreateSession(UGUI user, string clientIPAddress);
        public abstract UserSessionInfo CreateSession(UGUI user, string clientIPAddress, UUID sessionID, UUID secureSessionID);

        #region Session access
        public abstract UserSessionInfo this[UUID sessionID] { get; }
        public abstract List<UserSessionInfo> this[UGUI user] { get; }
        public abstract bool TryGetValue(UUID sessionID, out UserSessionInfo sessionInfo);
        public abstract bool TryGetSecureValue(UUID secureSessionID, out UserSessionInfo sessionInfo);
        public abstract bool ContainsKey(UUID sessionID);
        public abstract bool ContainsKey(UGUI user);
        public abstract bool Remove(UUID sessionID);
        public abstract bool Remove(UGUI user);
        #endregion

        #region Session variable access
        public abstract string this[UUID sessionID, string assoc, string varname]
        {
            get; set;
        }
        public abstract void SetExpiringValue(UUID sessionID, string assoc, string varname, string value, TimeSpan span);
        public abstract bool TryGetValueExtendLifetime(UUID sessionID, string assoc, string varname, TimeSpan span, out UserSessionInfo.Entry value);
        public bool TryGetValue(UUID sessionID, string assoc, string varname, out string value)
        {
            UserSessionInfo.Entry entry;
            if(TryGetValue(sessionID, assoc, varname, out entry))
            {
                value = entry.Value;
                return true;
            }
            value = default(string);
            return false;
        }
        public abstract bool TryGetValue(UUID sessionID, string assoc, string varname, out UserSessionInfo.Entry value);
        public abstract bool ContainsKey(UUID sessionID, string assoc, string varname);
        public abstract bool Remove(UUID sessionID, string assoc, string varname);
        #endregion

        public void SetExpiringValue(UUID sessionID, KnownUserSessionInfoVariables varid, string value, TimeSpan span)
        {
            string assoc;
            string varname;
            if(!UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname))
            {
                throw new ArgumentOutOfRangeException(nameof(varid));
            }
            SetExpiringValue(sessionID, assoc, varname, value, span);
        }

        public bool TryGetValueExtendLifetime(UUID sessionID, KnownUserSessionInfoVariables varid, TimeSpan span, out UserSessionInfo.Entry value)
        {
            string assoc;
            string varname;
            value = default(UserSessionInfo.Entry);
            return UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname) && TryGetValueExtendLifetime(sessionID, assoc, varname, span, out value);
        }

        public string this[UUID sessionID, KnownUserSessionInfoVariables varid]
        {
            get
            {
                string assoc;
                string varname;
                string value;
                if(!UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname) ||
                    !TryGetValue(sessionID, assoc, varname, out value))
                {
                    throw new KeyNotFoundException();
                }
                return value;
            }

            set
            {
                string assoc;
                string varname;
                if (!UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname))
                {
                    throw new KeyNotFoundException();
                }
                this[sessionID, assoc, varname] = value;
            }
        }

        public bool TryGetValue(UUID sessionID, KnownUserSessionInfoVariables varid, out string value)
        {
            value = default(string);
            string assoc;
            string varname;
            return UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname) && TryGetValue(sessionID, assoc, varname, out value);
        }

        public bool TryGetValue(UUID sessionID, KnownUserSessionInfoVariables varid, out UserSessionInfo.Entry value)
        {
            value = default(UserSessionInfo.Entry);
            string assoc;
            string varname;
            return UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname) && TryGetValue(sessionID, assoc, varname, out value);
        }

        public bool ContainsKey(UUID sessionID, KnownUserSessionInfoVariables varid)
        {
            string assoc;
            string varname;
            return UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname) && ContainsKey(sessionID, assoc, varname);
        }

        public bool Remove(UUID sessionID, KnownUserSessionInfoVariables varid)
        {
            string assoc;
            string varname;
            return UserSessionInfo.TryGetVarInfo(varid, out assoc, out varname) && Remove(sessionID, assoc, varname);
        }
    }
}
