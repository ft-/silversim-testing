/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using Nini.Config;
using SilverSim.BackendConnectors.Robust.Asset;
using SilverSim.BackendConnectors.Robust.Inventory;
using SilverSim.LL.Core;
using SilverSim.LL.Messages.Agent;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.StructuredData.Agent;
using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using ThreadedClasses;
using Nwc.XmlRpc;

namespace SilverSim.BackendHandlers.Robust.Simulation
{
    #region Service Implementation
    public class PostAgentHGDirectHandler : PostAgentHandler
    {
        private HttpXmlRpcHandler m_XmlRpcServer;

        public PostAgentHGDirectHandler(IConfig ownSection)
            : base("/foreignagent/", ownSection)
        {

        }

        public new void Startup(ConfigurationLoader loader)
        {
            base.Startup(loader);
            m_Log.Info("Initializing DirectHG XMLRPC handlers");
            m_XmlRpcServer = loader.GetService<HttpXmlRpcHandler>("XmlRpcServer");
            m_XmlRpcServer.XmlRpcMethods.Add("link_region", LinkRegion);
            m_XmlRpcServer.XmlRpcMethods.Add("get_region", GetRegion);
        }

        public new void Shutdown()
        {
            m_XmlRpcServer.XmlRpcMethods.Remove("link_region");
            m_XmlRpcServer.XmlRpcMethods.Remove("get_region");
            base.Shutdown();
        }

        protected new void CheckScenePerms(UUID sceneID)
        {
            if (!m_ServerParams.GetBoolean(sceneID, "HGDirectEnabled"))
            {
                throw new InvalidOperationException("No HG Direct access to scene");
            }
        }

        XmlRpcResponse LinkRegion(XmlRpcRequest req)
        {
            string region_name = string.Empty;
            try
            {
                region_name = (string)(((IDictionary)req.Params[0])["region_name"]);
            }
            catch
            {
                region_name = "";
            }
            Dictionary<string, object> resdata = new Dictionary<string, object>();
            resdata["result"] = false;
            if (string.IsNullOrEmpty(region_name))
            {
                region_name = m_ServerParams.GetString(UUID.Zero, "DefaultHGRegion", region_name);
            }

            if(!string.IsNullOrEmpty(region_name))
            {
                try
                {
                    SceneInterface s = Scene.Management.Scene.SceneManager.Scenes[region_name];
                    if (m_ServerParams.GetBoolean(s.ID, "HGDirectEnabled"))
                    {
                        resdata["uuid"] = s.ID;
                        resdata["handle"] = s.RegionData.Location.RegionHandle;
                        resdata["region_image"] = "";
                        resdata["external_name"] = s.RegionData.ServerURI + " " + s.RegionData.Name;
                        resdata["result"] = true;
                    }
                }
                catch
                {
                }
            }
            XmlRpcResponse res = new XmlRpcResponse();
            res.Value = resdata;
            return res;
        }

        XmlRpcResponse GetRegion(XmlRpcRequest req)
        {
            UUID region_uuid = new UUID((string)(((IDictionary)req.Params[0])["region_uuid"]));
            Dictionary<string, object> resdata = new Dictionary<string, object>();
            resdata["result"] = false;
            try
            {
                SceneInterface s = Scene.Management.Scene.SceneManager.Scenes[region_uuid];
                if (m_ServerParams.GetBoolean(s.ID, "HGDirectEnabled"))
                {
                    resdata["uuid"] = s.ID;
                    resdata["handle"] = s.RegionData.Location.RegionHandle;
                    resdata["x"] = s.RegionData.Location.X;
                    resdata["y"] = s.RegionData.Location.Y;
                    resdata["region_name"] = s.RegionData.Name;
                    Uri serverURI = new Uri(s.RegionData.ServerURI);
                    resdata["hostname"] = serverURI.Host;
                    resdata["http_port"] = s.RegionData.ServerHttpPort;
                    resdata["internal_port"] = s.RegionData.ServerPort;
                    resdata["server_uri"] = s.RegionData.ServerURI;
                    resdata["result"] = true;
                }
            }
            catch
            {
            }
            XmlRpcResponse res = new XmlRpcResponse();
            res.Value = resdata;
            return res;
        }
    }
    #endregion

    #region Service Factory
    public class PostAgentDirectHGHandlerFactory : IPluginFactory
    {
        public PostAgentDirectHGHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new PostAgentHGDirectHandler(ownSection);
        }
    }
    #endregion

}
