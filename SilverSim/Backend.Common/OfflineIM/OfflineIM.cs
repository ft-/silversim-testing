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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.ComponentModel;

namespace SilverSim.Backend.Common.OfflineIM
{
    [Description("Offline IM Handler")]
    [PluginName("OfflineIMHandler")]
    public sealed class OfflineIM : IPlugin, IPluginShutdown
    {
#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("OFFLINE IM");
#endif
        private readonly string m_AvatarNameServiceName;
        private readonly string m_OfflineIMServiceName;

        private AvatarNameServiceInterface m_AvatarNameService;
        private OfflineIMServiceInterface m_OfflineIMService;
        private IMRouter m_IMRouter;

        public OfflineIM(IConfig ownConfig)
        {
            m_AvatarNameServiceName = ownConfig.GetString("GridAvatarNameService", string.Empty);
            m_OfflineIMServiceName = ownConfig.GetString("OfflineIMService", string.Empty);
            if (string.IsNullOrEmpty(m_AvatarNameServiceName))
            {
                throw new ArgumentException("GridAvatarNameService not set");
            }
            if (string.IsNullOrEmpty(m_OfflineIMServiceName))
            {
                throw new ArgumentException("OfflineIMService not set");
            }
        }

        public bool Send(GridInstantMessage im)
        {
            if(im.NoOfflineIMStore)
            {
                return false;
            }
            if(im.IsOffline)
            {
                return false;
            }
            switch(im.Dialog)
            {
                case GridInstantMessageDialog.MessageFromAgent:
                case GridInstantMessageDialog.MessageFromObject:
                case GridInstantMessageDialog.GroupNotice:
                case GridInstantMessageDialog.GroupInvitation:
                case GridInstantMessageDialog.InventoryOffered:
                    break;

                default:
                    throw new IMSendFailedException();
            }

            UGUIWithName uui;
            try
            {
                uui = m_AvatarNameService[im.ToAgent];
            }
            catch
            {
                return false;
            }

            im.ToAgent = uui;

            try
            {
                m_OfflineIMService.StoreOfflineIM(im);
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
#if DEBUG
                m_Log.Warn("Storing of IM failed", e);
#endif
                return false;
            }

            if(im.Dialog == GridInstantMessageDialog.MessageFromAgent)
            {
                try
                {
                    var response_im = new GridInstantMessage
                    {
                        FromAgent = uui,
                        ToAgent = im.FromAgent,
                        Dialog = GridInstantMessageDialog.BusyAutoResponse,
                        IsFromGroup = false,
                        Message = "User is not logged in. Message saved.",
                        IMSessionID = im.IMSessionID,
                        IsOffline = false,
                        NoOfflineIMStore = true,
                        IsSystemMessage = true
                    };
                    m_IMRouter.SendWithResultDelegate(response_im);
                }
                catch
                {
                    /* exception intentionally ignored */
                }
            }
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_IMRouter = loader.IMRouter;
            m_AvatarNameService = loader.GetService<AvatarNameServiceInterface>(m_AvatarNameServiceName);
            m_OfflineIMService = loader.GetService<OfflineIMServiceInterface>(m_OfflineIMServiceName);
            m_IMRouter.OfflineIM.Add(Send);
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_IMRouter.OfflineIM.Remove(Send);
        }
    }
}
