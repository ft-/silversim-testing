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
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Backend.Common.OfflineIM
{
    #region Service implementation
    [Description("Offline IM Handler")]
    public sealed class OfflineIM : IPlugin, IPluginShutdown
    {
#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("OFFLINE IM");
#endif
        readonly string m_AvatarNameServiceName;
        readonly string m_OfflineIMServiceName;

        AvatarNameServiceInterface m_AvatarNameService;
        OfflineIMServiceInterface m_OfflineIMService;
        IMRouter m_IMRouter;

        public OfflineIM(string avatarNameServiceName, string offlineIMServiceName)
        {
            m_AvatarNameServiceName = avatarNameServiceName;
            m_OfflineIMServiceName = offlineIMServiceName;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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

            UUI uui;
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
                    GridInstantMessage response_im = new GridInstantMessage();
                    response_im.FromAgent = im.ToAgent;
                    response_im.ToAgent = im.FromAgent;
                    response_im.Dialog = GridInstantMessageDialog.BusyAutoResponse;
                    response_im.IsFromGroup = false;
                    response_im.Message = "User is not logged in. Message saved.";
                    response_im.IMSessionID = im.IMSessionID;
                    response_im.IsOffline = false;
                    response_im.NoOfflineIMStore = true;
                    response_im.IsSystemMessage = true;
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

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_IMRouter.OfflineIM.Remove(Send);
        }
    }
    #endregion

    #region Factory
    [PluginName("OfflineIMHandler")]
    public class OfflineIMFactory : IPluginFactory
    {
        public OfflineIMFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownConfig)
        {
            string avatarNameServiceName = ownConfig.GetString("GridAvatarNameService", string.Empty);
            string offlineIMServiceName = ownConfig.GetString("OfflineIMService", string.Empty);
            if (string.IsNullOrEmpty(avatarNameServiceName))
            {
                throw new ArgumentException("GridAvatarNameService not set");
            }
            if (string.IsNullOrEmpty(offlineIMServiceName))
            {
                throw new ArgumentException("OfflineIMService not set");
            }
            return new OfflineIM(avatarNameServiceName, offlineIMServiceName);
        }
    }
    #endregion
}
