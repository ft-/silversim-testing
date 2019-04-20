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
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.IM
{
    [Description("Grid IM Router")]
    [PluginName("GridIMRouter")]
    public sealed class GridIMRouter : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("GRID IM ROUTER");
        private string m_AvatarNameServiceNames = string.Empty;
        private readonly AvatarNameServiceInterface m_AvatarNameService;
        private readonly RwLockedList<AvatarNameServiceInterface> m_AvatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
        private List<IUserAgentServicePlugin> m_UserAgentServicePlugins;
        private ConfigurationLoader m_Loader;

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        public GridIMRouter(IConfig config)
        {
            m_AvatarNameServiceNames = config.GetString("AvatarNameServices", string.Empty);
            m_AvatarNameService = new AggregatingAvatarNameService(m_AvatarNameServices);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Loader = loader;
            foreach (string name in m_AvatarNameServiceNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if(!string.IsNullOrEmpty(name))
                {
                    AvatarNameServiceInterface avatarNameService;
                    loader.GetService(name, out avatarNameService);
                    m_AvatarNameServices.Add(avatarNameService);
                }
            }
            m_UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            loader.IMRouter.GridIM.Add(RouteIM);
        }

        private bool RouteIM(GridInstantMessage im)
        {
            if(im.ToAgent.HomeURI == null)
            {
                return false;
            }
            string homeUri = im.ToAgent.HomeURI.ToString();
            var heloheaders = ServicePluginHelo.HeloRequest(homeUri);
            UserAgentServiceInterface userAgentService = null;
            foreach (IUserAgentServicePlugin userAgentPlugin in m_UserAgentServicePlugins)
            {
                if (userAgentPlugin.IsProtocolSupported(homeUri, heloheaders))
                {
                    userAgentService = userAgentPlugin.Instantiate(homeUri);
                }
            }

            if (userAgentService == null)
            {
                return false;
            }

            IMServiceInterface imService;
            try
            {
                imService = userAgentService.GetIMService(im.ToAgent.ID);
            }
            catch(Exception e)
            {
                m_Log.Debug($"Failed to deliver IM to {homeUri}: User lookup failed", e);
                return false;
            }

            try
            {
                imService.Send(im);
            }
            catch(Exception e)
            {
                m_Log.Debug($"Failed to deliver IM to {homeUri}: Delivery failed", e);
                return false;
            }

            return true;
        }

        public void Shutdown()
        {
            m_Loader.IMRouter.GridIM.Remove(RouteIM);
        }
    }
}
