// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using System.ComponentModel;
using System;
using SilverSim.ServiceInterfaces.AvatarName;
using System.Collections.Generic;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types.StructuredData.XmlRpc;
using SilverSim.Types;
using SilverSim.Types.Presence;
using SilverSim.Main.Common.HttpServer;

namespace SilverSim.Grid.BasicLandtool
{
    [Description("Basic Landtool")]
    public class Landtool : IPlugin
    {
        readonly string m_PresenceServiceName;
        PresenceServiceInterface m_PresenceService;
        BaseHttpServer m_HttpServer;

        public Landtool(IConfig ownSection)
        {
            m_PresenceServiceName = ownSection.GetString("PresenceService", "PresenceService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_HttpServer = loader.HttpServer;
            m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
            loader.XmlRpcServer.XmlRpcMethods.Add("preflightBuyLandPrep", HandlePreFlightBuyLandPrep);
        }

        XmlRpc.XmlRpcResponse HandlePreFlightBuyLandPrep(XmlRpc.XmlRpcRequest req)
        {
            Map structParam;
            UUID agentId;
            UUID secureSessionId;
            if(!req.Params.TryGetValue(0, out structParam) ||
                !structParam.TryGetValue("agentId", out agentId) ||
                !structParam.TryGetValue("secureSessionId", out secureSessionId))
            {
                throw new XmlRpc.XmlRpcFaultException(4, "Missing parameters");
            }

            bool validated = false;
            foreach(PresenceInfo pinfo in m_PresenceService[agentId])
            {
                if(pinfo.SecureSessionID == secureSessionId)
                {
                    validated = true;
                    break;
                }
            }

            Map resdata = new Map();
            if(!validated)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", "\n\nUnable to Authenticate\n\nClick URL for more info.");
                resdata.Add("errorURI", m_HttpServer.ServerURI);
            }
            else
            {
                Map membership_level = new Map();
                membership_level.Add("id", UUID.Zero);
                membership_level.Add("description", "some level");
                Map membership_levels = new Map();
                membership_levels.Add("level", membership_level);

                Map landUse = new Map();
                landUse.Add("upgrade", false);
                landUse.Add("action", m_HttpServer.ServerURI);
                Map currency = new Map();
                currency.Add("estimatedCost", "200.00");

                Map membership = new Map();
                membership.Add("upgrade", false);
                membership.Add("action", m_HttpServer.ServerURI);
                membership.Add("levels", membership_levels);

                resdata.Add("success", true);
                resdata.Add("membership", membership);
                resdata.Add("landUse", landUse);
                resdata.Add("currency", currency);
                resdata.Add("confirm", string.Empty);
            }

            return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
        }
    }

    [PluginName("BasicLandtool")]
    public class LandtoolFactory : IPluginFactory
    {
        public LandtoolFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new Landtool(ownSection);
        }
    }
}
