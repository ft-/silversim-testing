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
using SilverSim.Types.Agent;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.UserSession
{
    public class StandalonePresenceService : IPresenceServiceInterface
    {
        private readonly UserSessionServiceInterface m_UserSessionService;
        private readonly UserAccountServiceInterface m_UserAccountService;
        private readonly UUID m_SessionID;
        private readonly UGUI m_User;
        private List<IUserSessionStatusHandler> m_UserSessionStatusServices;

        public StandalonePresenceService(
            UserAccountServiceInterface userAccountService, UGUI user,
            UserSessionServiceInterface userSessionService, UUID sessionID,
            List<IUserSessionStatusHandler> UserSessionStatusServices)
        {
            m_UserAccountService = userAccountService;
            m_User = user;
            m_UserSessionService = userSessionService;
            m_SessionID = sessionID;
            m_UserSessionStatusServices = UserSessionStatusServices;
        }

        private void SendOffline()
        {
            foreach (IUserSessionStatusHandler handler in m_UserSessionStatusServices)
            {
                try
                {
                    handler.UserSessionLogout(m_SessionID, m_User);
                }
                catch
                {
                    /* intentionally ignored */
                }
            }
        }

        public bool Logout()
        {
            bool f = m_UserSessionService.Remove(m_SessionID);
            if (f)
            {
                try
                {
                    m_UserAccountService.LoggedOut(m_User.ID);
                }
                finally
                {
                    SendOffline();
                }
            }
            return f;
        }

        public bool Logout(UUID regionID, Vector3 position, Vector3 lookAt)
        {
            bool f = m_UserSessionService.Remove(m_SessionID);
            if (f)
            {
                try
                {
                    m_UserAccountService.LoggedOut(m_User.ID, new UserRegionData
                    {
                        RegionID = regionID,
                        Position = position,
                        LookAt = lookAt
                    });
                }
                finally
                {
                    SendOffline();
                }
            }
            return f;
        }

        public bool Report(UUID regionID)
        {
            return false;
        }
    }
}
