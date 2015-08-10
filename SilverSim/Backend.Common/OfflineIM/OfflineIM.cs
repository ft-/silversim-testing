﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;

namespace SilverSim.Backend.Common.OfflineIM
{
    #region Service implementation
    class OfflineIM : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("OFFLINE IM");
        string m_AvatarNameServiceName;
        string m_OfflineIMServiceName;

        AvatarNameServiceInterface m_AvatarNameService;
        OfflineIMServiceInterface m_OfflineIMService;

        public OfflineIM(string avatarNameServiceName, string offlineIMServiceName)
        {
            m_AvatarNameServiceName = avatarNameServiceName;
            m_OfflineIMServiceName = offlineIMServiceName;
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

            UUI uui;
            try
            {
                uui = m_AvatarNameService[im.ToAgent];
            }
            catch
            {
                return false;
            }

            try
            {
                m_OfflineIMService.storeOfflineIM(im);
            }
            catch(Exception e)
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
                    IMRouter.SendWithResultDelegate(response_im);
                }
                catch
                {

                }
            }
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_AvatarNameService = loader.GetService<AvatarNameServiceInterface>(m_AvatarNameServiceName);
            m_OfflineIMService = loader.GetService<OfflineIMServiceInterface>(m_OfflineIMServiceName);
            IMRouter.OfflineIM.Add(Send);
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
            IMRouter.OfflineIM.Remove(Send);
        }
    }
    #endregion

    #region Factory
    [PluginName("OfflineIMHandler")]
    public class OfflineIMFactory : IPluginFactory
    {

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownConfig)
        {
            string avatarNameServiceName = ownConfig.GetString("GridAvatarNameService", "");
            string offlineIMServiceName = ownConfig.GetString("OfflineIMService", "");
            if (string.IsNullOrEmpty(avatarNameServiceName))
            {
                throw new Exception("GridAvatarNameService not set");
            }
            if (string.IsNullOrEmpty(offlineIMServiceName))
            {
                throw new Exception("OfflineIMService not set");
            }
            return new OfflineIM(avatarNameServiceName, offlineIMServiceName);
        }
    }
    #endregion
}
