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

namespace SilverSim.Database.Memory.UserSession
{
    [PluginName("UserSession")]
    public sealed class MemoryUserSessionService : UserSessionServiceInterface, IPlugin
    {
        private readonly Dictionary<UUID, UserSessionInfo> m_UserSessions = new Dictionary<UUID, UserSessionInfo>();
        private readonly object m_UserSessionLock = new object();

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override UserSessionInfo this[UUID sessionID]
        {
            get
            {
                lock (m_UserSessionLock)
                {
                    UserSessionInfo info;
                    if (m_UserSessions.TryGetValue(sessionID, out info))
                    {
                        return new UserSessionInfo(info);
                    }
                }
                throw new KeyNotFoundException();
            }
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
                string value;
                lock(m_UserSessionLock)
                {
                    UserSessionInfo info;
                    if(m_UserSessions.TryGetValue(sessionID, out info) &&
                        info.DynamicData.TryGetValue($"{assoc}/{varname}", out value))
                    {
                        return value;
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
                        info.DynamicData[$"{assoc}/{varname}"] = value;
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

        public override UserSessionInfo CreateSession(UGUI user)
        {
            var userSessionInfo = new UserSessionInfo
            {
                User = user,
                SessionID = UUID.Random,
                SecureSessionID = UUID.Random
            };
            lock(m_UserSessionLock)
            {
                m_UserSessions.Add(userSessionInfo.SessionID, new UserSessionInfo(userSessionInfo));
            }
            return userSessionInfo;
        }

        public override UserSessionInfo CreateSession(UGUI user, UUID sessionID, UUID secureSessionID)
        {
            var userSessionInfo = new UserSessionInfo
            {
                User = user,
                SessionID = sessionID,
                SecureSessionID = secureSessionID
            };
            lock (m_UserSessionLock)
            {
                m_UserSessions.Add(userSessionInfo.SessionID, new UserSessionInfo(userSessionInfo));
            }
            return userSessionInfo;
        }

        public override bool Remove(UUID sessionID)
        {
            lock(m_UserSessionLock)
            {
                return m_UserSessions.Remove(sessionID);
            }
        }

        public override bool Remove(UUID sessionID, string assoc, string varname)
        {
            lock(m_UserSessionLock)
            {
                UserSessionInfo info;
                return m_UserSessions.TryGetValue(sessionID, out info) && info.DynamicData.Remove($"{assoc}/{varname}");
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

        public override bool TryGetValue(UUID sessionID, string assoc, string varname, out string value)
        {
            value = default(string);
            lock(m_UserSessionLock)
            {
                UserSessionInfo info;
                return m_UserSessions.TryGetValue(sessionID, out info) && info.DynamicData.TryGetValue($"{assoc}/{varname}", out value);
            }
        }
    }
}
