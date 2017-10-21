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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using SilverSim.Types.StructuredData.XmlRpc;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace SilverSim.Grid.BasicLandtool
{
    [Description("Basic Landtool")]
    [PluginName("BasicLandtool")]
    public class Landtool : IPlugin, IGridInfoServiceInterface
    {
        private readonly string m_PresenceServiceName;
        private PresenceServiceInterface m_PresenceService;
        private BaseHttpServer m_HttpServer;

        public Landtool(IConfig ownSection)
        {
            m_PresenceServiceName = ownSection.GetString("PresenceService", "PresenceService");
        }

        public void GetGridInfo(Dictionary<string, string> dict)
        {
            dict["economy"] = m_HttpServer.ServerURI;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_HttpServer = loader.HttpServer;
            m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
            loader.XmlRpcServer.XmlRpcMethods.Add("preflightBuyLandPrep", HandlePreFlightBuyLandPrep);
        }

        private static CultureInfo GetLanguageCulture(string language)
        {
            try
            {
                return new CultureInfo(language);
            }
            catch
            {
                return new CultureInfo("en");
            }
        }

        private XmlRpc.XmlRpcResponse HandlePreFlightBuyLandPrep(XmlRpc.XmlRpcRequest req)
        {
            Map structParam;
            UUID agentId;
            UUID secureSessionId;
            IValue language;
            if(!req.Params.TryGetValue(0, out structParam) ||
                !structParam.TryGetValue("agentId", out agentId) ||
                !structParam.TryGetValue("secureSessionId", out secureSessionId) ||
                !structParam.TryGetValue("language", out language))
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

            var resdata = new Map();
            if(!validated)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", this.GetLanguageString(GetLanguageCulture(language.ToString()), "UnableToAuthenticate", "Unable to authenticate."));
                resdata.Add("errorURI", m_HttpServer.ServerURI);
            }
            else
            {
                var membership_level = new Map
                {
                    { "id", UUID.Zero },
                    { "description", "some level" }
                };
                var membership_levels = new Map
                {
                    ["level"] = membership_level
                };
                var landUse = new Map
                {
                    { "upgrade", false },
                    { "action", m_HttpServer.ServerURI }
                };
                var currency = new Map
                {
                    { "estimatedCost", "200.00" }
                };
                var membership = new Map
                {
                    { "upgrade", false },
                    { "action", m_HttpServer.ServerURI },
                    { "levels", membership_levels }
                };
                resdata.Add("success", true);
                resdata.Add("membership", membership);
                resdata.Add("landUse", landUse);
                resdata.Add("currency", currency);
                resdata.Add("confirm", string.Empty);
            }

            return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
        }


        private XmlRpc.XmlRpcResponse HandleBuyLandPrep(XmlRpc.XmlRpcRequest req)
        {
            Map structParam;
            UUID agentId;
            UUID secureSessionId;
            IValue language;
            IValue currencyBuy;
            IValue confirm;
            if (!req.Params.TryGetValue(0, out structParam) ||
                !structParam.TryGetValue("agentId", out agentId) ||
                !structParam.TryGetValue("secureSessionId", out secureSessionId) ||
                !structParam.TryGetValue("language", out language) ||
                !structParam.TryGetValue("currencyBuy", out currencyBuy) ||
                !structParam.TryGetValue("confirm", out confirm))
            {
                throw new XmlRpc.XmlRpcFaultException(4, "Missing parameters");
            }
            bool validated = false;
            foreach (PresenceInfo pinfo in m_PresenceService[agentId])
            {
                if (pinfo.SecureSessionID == secureSessionId)
                {
                    validated = true;
                    break;
                }
            }

            var resdata = new Map();
            if (!validated)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", this.GetLanguageString(GetLanguageCulture(language.ToString()), "UnableToAuthenticate", "Unable to authenticate."));
                resdata.Add("errorURI", m_HttpServer.ServerURI);
            }
            else
            {
                resdata.Add("success", true);
            }

            return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
        }
    }
}
