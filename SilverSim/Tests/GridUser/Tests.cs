// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.GridUser;
using System;
using System.Reflection;

namespace SilverSim.Tests.GridUser
{
    public class Tests : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        GridUserServiceInterface m_GridUserSuT;
        GridUserServiceInterface m_GridUserBackend;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_GridUserSuT = loader.GetService<GridUserServiceInterface>(config.GetString("ServiceUnderTest"));
            /* we need the backend service so that we can create the entries we need for testing */
            m_GridUserBackend = loader.GetService<GridUserServiceInterface>(config.GetString("Backend"));
        }

        bool CompareGridUserInfos(GridUserInfo gui1, GridUserInfo gui2)
        {
            bool result = true;
            if (gui1.HomeLookAt != gui2.HomeLookAt)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.HomeLookAt is not equal ({0} != {1})", gui1.HomeLookAt, gui2.HomeLookAt);
            }
            if (gui1.HomePosition != gui2.HomePosition)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.HomePosition is not equal ({0} != {1})", gui1.HomePosition, gui2.HomePosition);
            }
            if (gui1.HomeRegionID != gui2.HomeRegionID)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.HomeRegionID is not equal ({0} != {1})", gui1.HomeRegionID, gui2.HomeRegionID);
            }
            if (gui1.IsOnline != gui2.IsOnline)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.IsOnline is not equal ({0} != {1})", gui1.IsOnline, gui2.IsOnline);
            }
            if ((ulong)gui1.LastLogin != (ulong)gui2.LastLogin)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.LastLogin is not equal ({0} != {1})", gui1.LastLogin, gui2.LastLogin);
            }
            if ((ulong)gui1.LastLogout != (ulong)gui2.LastLogout)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.LastLogout is not equal ({0} != {1})", gui1.LastLogout, gui2.LastLogout);
            }
            if (gui1.LastLookAt != gui2.LastLookAt)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.LastLookAt is not equal ({0} != {1})", gui1.LastLookAt, gui2.LastLookAt);
            }
            if (gui1.LastPosition != gui2.LastPosition)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.LastPosition is not equal ({0} != {1})", gui1.LastPosition, gui2.LastPosition);
            }
            if (gui1.LastRegionID != gui2.LastRegionID)
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.LastRegionID is not equal ({0} != {1})", gui1.LastRegionID, gui2.LastRegionID);
            }
            if (!gui1.User.Equals(gui2.User))
            {
                result = false;
                m_Log.WarnFormat("GridUserInfo.User is not equal ({0} != {1})", gui1.User, gui2.User);
            }
            return result;
        }

        public bool Run()
        {
            UUI hgUUI = new UUI(UUID.Parse("11223344-1122-1122-1122-112233445566"), "First", "Last", new Uri("http://example.com/"));
            UUI localUUI = new UUI(UUID.Parse("44332211-2211-2211-2211-665544332211"));
            UUI residentHGUUI = new UUI(UUID.Parse("44332211-2211-2211-2211-665544332211"), "The", "Resident", new Uri("http://residents.home/"));
            UUID region1ID = new UUID("11112222-1111-2222-3333-112233445566");
            UUID region2ID = new UUID("33334444-1111-2222-3333-112233445566");

            m_GridUserBackend.LoggedInAdd(hgUUI);
            m_GridUserBackend.LoggedInAdd(localUUI);

            m_Log.Info("Testing local UUI vs. UUID requests");
            GridUserInfo gui_uui = m_GridUserSuT[localUUI];
            GridUserInfo gui_uuid = m_GridUserSuT[localUUI.ID];
            if (!CompareGridUserInfos(gui_uui, gui_uuid))
            {
                return false;
            }

            m_Log.Info("Testing resident HG UUI vs. resident UUID requests");
            gui_uui = m_GridUserSuT[residentHGUUI];
            if (!CompareGridUserInfos(gui_uui, gui_uuid))
            {
                return false;
            }

            m_Log.Info("Testing HG UUI vs. UUID requests");
            gui_uui = m_GridUserSuT[hgUUI];
            gui_uuid = m_GridUserSuT[hgUUI.ID];
            if (!CompareGridUserInfos(gui_uui, gui_uuid))
            {
                return false;
            }

            m_Log.Info("Testing SetPosition of GridUser");
            m_GridUserSuT.SetPosition(hgUUI, region2ID, Vector3.Zero, Vector3.Zero);
            m_GridUserSuT.SetPosition(localUUI, region1ID, Vector3.Zero, Vector3.Zero);

            gui_uui = m_GridUserSuT[hgUUI];
            gui_uuid = m_GridUserSuT[localUUI];

            if (!gui_uui.IsOnline)
            {
                m_Log.Info("Logout of HG UUI should not happen.");
                return false;
            }
            if (!gui_uuid.IsOnline)
            {
                m_Log.Info("Logout of local UUID should not happen.");
                return false;
            }
            if (gui_uui.LastRegionID != region2ID)
            {
                m_Log.InfoFormat("LastRegionID was not stored for HG UUI. {0} != {1}", gui_uui.LastRegionID, region2ID);
                return false;
            }
            if (gui_uuid.LastRegionID != region1ID)
            {
                m_Log.InfoFormat("LastRegionID was not stored for Local UUID. {0} != {1}", gui_uuid.LastRegionID, region1ID);
                return false;
            }

            m_Log.Info("Testing SetHome of GridUser");
            m_GridUserSuT.SetHome(hgUUI, region2ID, Vector3.Zero, Vector3.Zero);
            m_GridUserSuT.SetHome(localUUI, region1ID, Vector3.Zero, Vector3.Zero);

            gui_uui = m_GridUserSuT[hgUUI];
            gui_uuid = m_GridUserSuT[localUUI];

            if (!gui_uui.IsOnline)
            {
                m_Log.Info("Logout of HG UUI should not happen.");
                return false;
            }
            if (!gui_uuid.IsOnline)
            {
                m_Log.Info("Logout of local UUID should not happen.");
                return false;
            }
            if (gui_uui.HomeRegionID != region2ID)
            {
                m_Log.InfoFormat("HomeRegionID was not stored for HG UUI. {0} != {1}", gui_uui.HomeRegionID, region2ID);
                return false;
            }
            if (gui_uuid.HomeRegionID != region1ID)
            {
                m_Log.InfoFormat("HomeRegionID was not stored for Local UUID. {0} != {1}", gui_uuid.HomeRegionID, region1ID);
                return false;
            }

            m_Log.Info("Testing Logout of GridUser");
            m_GridUserSuT.LoggedOut(hgUUI, region1ID, Vector3.Zero, Vector3.Zero);
            m_GridUserSuT.LoggedOut(localUUI, region2ID, Vector3.Zero, Vector3.Zero);

            gui_uui = m_GridUserSuT[hgUUI];
            gui_uuid = m_GridUserSuT[localUUI];

            if(gui_uui.IsOnline)
            {
                m_Log.Info("Logout of HG UUI was not stored.");
                return false;
            }
            if (gui_uuid.IsOnline)
            {
                m_Log.Info("Logout of local UUID was not stored.");
                return false;
            }
            if (gui_uui.LastRegionID != region1ID)
            {
                m_Log.InfoFormat("LastRegionID was not stored for HG UUI. {0} != {1}", gui_uui.LastRegionID, region1ID);
                return false;
            }
            if(gui_uuid.LastRegionID != region2ID)
            {
                m_Log.InfoFormat("LastRegionID was not stored for Local UUID. {0} != {1}", gui_uuid.LastRegionID, region2ID);
                return false;
            }
            if (gui_uui.HomeRegionID != region2ID)
            {
                m_Log.InfoFormat("HomeRegionID was changed for HG UUI. {0} != {1}", gui_uui.HomeRegionID, region2ID);
                return false;
            }
            if (gui_uuid.HomeRegionID != region1ID)
            {
                m_Log.InfoFormat("HomeRegionID was changed for Local UUID. {0} != {1}", gui_uuid.HomeRegionID, region1ID);
                return false;
            }

            m_Log.Info("Testing SetHome of GridUser after Logout");
            m_GridUserSuT.SetHome(hgUUI, region1ID, Vector3.Zero, Vector3.Zero);
            m_GridUserSuT.SetHome(localUUI, region2ID, Vector3.Zero, Vector3.Zero);

            gui_uui = m_GridUserSuT[hgUUI];
            gui_uuid = m_GridUserSuT[localUUI];

            if (gui_uui.IsOnline)
            {
                m_Log.Info("Login of HG UUI should not happen.");
                return false;
            }
            if (gui_uuid.IsOnline)
            {
                m_Log.Info("Login of local UUID should not happen.");
                return false;
            }
            if (gui_uui.HomeRegionID != region1ID)
            {
                m_Log.InfoFormat("HomeRegionID was not stored for HG UUI. {0} != {1}", gui_uui.HomeRegionID, region1ID);
                return false;
            }
            if (gui_uuid.HomeRegionID != region2ID)
            {
                m_Log.InfoFormat("HomeRegionID was not stored for Local UUID. {0} != {1}", gui_uuid.HomeRegionID, region2ID);
                return false;
            }

            m_Log.Info("Testing final local UUI vs. UUID requests");
            gui_uui = m_GridUserSuT[localUUI];
            gui_uuid = m_GridUserSuT[localUUI.ID];
            if (!CompareGridUserInfos(gui_uui, gui_uuid))
            {
                return false;
            }

            m_Log.Info("Testing final resident HG UUI vs. resident UUID requests");
            gui_uui = m_GridUserSuT[residentHGUUI];
            if (!CompareGridUserInfos(gui_uui, gui_uuid))
            {
                return false;
            }

            m_Log.Info("Testing final HG UUI vs. UUID requests");
            gui_uui = m_GridUserSuT[hgUUI];
            gui_uuid = m_GridUserSuT[hgUUI.ID];
            if (!CompareGridUserInfos(gui_uui, gui_uuid))
            {
                return false;
            }

            return true;
        }
    }
}
