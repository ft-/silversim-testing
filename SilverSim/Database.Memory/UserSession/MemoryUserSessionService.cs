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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Types;
using SilverSim.Types.UserSession;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.UserSession
{
    [Description("Memory UserSession Backend")]
    [PluginName("UserSession")]
    public sealed class MemoryUserSessionService : UserSessionServiceInterface, IPlugin
    {
        private readonly Dictionary<UUID, UserSessionInfo> m_UserSessions = new Dictionary<UUID, UserSessionInfo>();
        private readonly Dictionary<UUID, UUID> m_UserSecureSessions = new Dictionary<UUID, UUID>();
        private readonly object m_UserSessionLock = new object();

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override List<UserSessionInfo> this[UGUI user]
        {
            get
            {
                var infos = new List<UserSessionInfo>();
                lock(m_UserSessionLock)
                {
                    foreach(UserSessionInfo info in m_UserSessions.Values)
                    {
                        if (info.User == user)
                        {
                            infos.Add(new UserSessionInfo(info));
                        }
                    }
                }
                return infos;
            }
        }

        public override string this[UUID sessionID, string assoc, string varname]
        {
            get
            {
                UserSessionInfo.Entry value;
                lock(m_UserSessionLock)
                {
                    UserSessionInfo info;
                    if(m_UserSessions.TryGetValue(sessionID, out info) &&
                        info.DynamicData.TryGetValue($"{assoc}/{varname}", out value) &&
                        (value.ExpiryDate == null || value.ExpiryDate.AsULong < Date.Now.AsULong))
                    {
                        return value.Value;
                    }
                }
                throw new KeyNotFoundException();
            }
            set
            {
                if(assoc.Contains("/"))
                {
                    throw new ArgumentOutOfRangeException(nameof(assoc));
                }
                if (varname.Contains("/"))
                {
                    throw new ArgumentOutOfRangeException(nameof(varname));
                }
                lock (m_UserSessionLock)
                {
                    UserSessionInfo info;
                    if(m_UserSessions.TryGetValue(sessionID, out info))
                    {
                        info.DynamicData[$"{assoc}/{varname}"] = new UserSessionInfo.Entry { Value = value };
                    }
                    else
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override bool ContainsKey(UUID sessionID)
        {
            lock (m_UserSessionLock)
            {
                return m_UserSessions.ContainsKey(sessionID);
            }
        }

        public override bool ContainsKey(UGUI user)
        {
            var infos = new List<UserSessionInfo>();
            lock (m_UserSessionLock)
            {
                foreach (UserSessionInfo info in m_UserSessions.Values)
                {
                    if (info.User == user)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool ContainsKey(UUID sessionID, string assoc, string varname)
        {
            lock(m_UserSessionLock)
            {
                UserSessionInfo info;
                return m_UserSessions.TryGetValue(sessionID, out info) && info.DynamicData.ContainsKey($"{assoc}/{varname}");
            }
        }

        public override UserSessionInfo CreateSession(UGUI user, string clientIPAddress, UUID sessionID, UUID secureSessionID)
        {
            var userSessionInfo = new UserSessionInfo
            {
                User = user,
                SessionID = sessionID,
                SecureSessionID = secureSessionID,
                ClientIPAddress = clientIPAddress
            };
            lock (m_UserSessionLock)
            {
                m_UserSessions.Add(userSessionInfo.SessionID, new UserSessionInfo(userSessionInfo));
                try
                {
                    m_UserSecureSessions.Add(userSessionInfo.SecureSessionID, userSessionInfo.SessionID);
                }
                catch
                {
                    m_UserSessions.Remove(userSessionInfo.SessionID);
                    throw;
                }
            }
            return userSessionInfo;
        }

        public override bool Remove(UUID sessionID)
        {
            lock(m_UserSessionLock)
            {
                UserSessionInfo info;
                if(m_UserSessions.TryGetValue(sessionID, out info))
                {
                    m_UserSecureSessions.Remove(info.SecureSessionID);
                }
                return m_UserSessions.Remove(sessionID);
            }
        }

        public override bool Remove(UGUI user)
        {
            List<UserSessionInfo> sessions = this[user];
            lock(m_UserSessionLock)
            {
                foreach(UserSessionInfo info in sessions)
                {
                    m_UserSessions.Remove(info.SessionID);
                }
            }
            return sessions.Count != 0;
        }

        public override bool Remove(UUID sessionID, string assoc, string varname)
        {
            lock(m_UserSessionLock)
            {
                UserSessionInfo info;
                return m_UserSessions.TryGetValue(sessionID, out info) && info.DynamicData.Remove($"{assoc}/{varname}");
            }
        }

        public override bool CompareAndRemove(UUID sessionID, string assoc, string varname, string value)
        {
            lock (m_UserSessionLock)
            {
                UserSessionInfo info;
                if (m_UserSessions.TryGetValue(sessionID, out info))
                {
                    UserSessionInfo.Entry val;
                    if(info.DynamicData.TryGetValue($"{assoc}/{varname}", out val) && value == val.Value)
                    {
                        return info.DynamicData.Remove($"{assoc}/{varname}");
                    }
                }
            }
            return false;
        }

        public override bool TryGetSecureValue(UUID secureSessionID, out UserSessionInfo sessionInfo)
        {
            lock (m_UserSessionLock)
            {
                UserSessionInfo info;
                UUID sessionID;
                if (m_UserSecureSessions.TryGetValue(secureSessionID, out sessionID) && m_UserSessions.TryGetValue(sessionID, out info))
                {
                    sessionInfo = new UserSessionInfo(info);
                    return true;
                }
            }
            sessionInfo = default(UserSessionInfo);
            return false;
        }

        public override void SetExpiringValue(UUID sessionID, string assoc, string varname, string value, TimeSpan span)
        {
            if (assoc.Contains("/"))
            {
                throw new ArgumentOutOfRangeException(nameof(assoc));
            }
            if (varname.Contains("/"))
            {
                throw new ArgumentOutOfRangeException(nameof(varname));
            }
            lock (m_UserSessionLock)
            {
                UserSessionInfo info;
                if (m_UserSessions.TryGetValue(sessionID, out info))
                {
                    info.DynamicData[$"{assoc}/{varname}"] = new UserSessionInfo.Entry { Value = value, ExpiryDate = Date.Now.Add(span) };
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
        }

        public override bool TryGetValueExtendLifetime(UUID sessionID, string assoc, string varname, TimeSpan span, out UserSessionInfo.Entry value)
        {
            value = default(UserSessionInfo.Entry);
            lock(m_UserSessionLock)
            {
                UserSessionInfo info;
                if(!m_UserSessions.TryGetValue(sessionID, out info))
                {
                    return false;
                }
                if(info.TryGetValue(assoc, varname, out value))
                {
                    if (value.ExpiryDate != null)
                    {
                        value.ExpiryDate = value.ExpiryDate.Add(span);
                    }
                    return true;
                }
                return false;
            }
        }

        public override bool TryCompareValueExtendLifetime(UUID sessionID, string assoc, string varname, string oldvalue, TimeSpan span, out UserSessionInfo.Entry value)
        {
            value = default(UserSessionInfo.Entry);
            lock (m_UserSessionLock)
            {
                UserSessionInfo info;
                if (!m_UserSessions.TryGetValue(sessionID, out info))
                {
                    return false;
                }
                if (info.TryGetValue(assoc, varname, out value) && value.Value == oldvalue)
                {
                    if (value.ExpiryDate != null)
                    {
                        value.ExpiryDate = value.ExpiryDate.Add(span);
                    }
                    return true;
                }
                return false;
            }
        }

        public override bool TryCompareAndChangeValueExtendLifetime(UUID sessionID, string assoc, string varname, string oldvalue, string newvalue, TimeSpan span, out UserSessionInfo.Entry value)
        {
            value = default(UserSessionInfo.Entry);
            lock (m_UserSessionLock)
            {
                UserSessionInfo info;
                if (!m_UserSessions.TryGetValue(sessionID, out info))
                {
                    return false;
                }
                if (info.TryGetValue(assoc, varname, out value) && value.Value == oldvalue)
                {
                    value.Value = newvalue;
                    if (value.ExpiryDate != null)
                    {
                        value.ExpiryDate = value.ExpiryDate.Add(span);
                    }
                    return true;
                }
                value = default(UserSessionInfo.Entry);
                return false;
            }
        }

        public override bool TryGetValue(UUID sessionID, out UserSessionInfo sessionInfo)
        {
            lock (m_UserSessionLock)
            {
                UserSessionInfo info;
                if(m_UserSessions.TryGetValue(sessionID, out info))
                {
                    sessionInfo = new UserSessionInfo(info);
                    return true;
                }
            }
            sessionInfo = default(UserSessionInfo);
            return false;
        }

        public override bool TryGetValue(UUID sessionID, string assoc, string varname, out UserSessionInfo.Entry value)
        {
            value = default(UserSessionInfo.Entry);
            lock (m_UserSessionLock)
            {
                UserSessionInfo info;
                bool f = m_UserSessions.TryGetValue(sessionID, out info) && info.TryGetValue(assoc, varname, out value);
                if(f)
                {
                    value = new UserSessionInfo.Entry(value);
                }
                return f;
            }
        }
    }
}
