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
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Types;
using SilverSim.Types.StructuredData.XmlRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace SilverSim.Viewer.Economy
{
    [Description("Simulator Currency Tool")]
    [PluginName("SimCurrencyTool")]
    public sealed class SimCurrencyTool : IPlugin, IGridInfoServiceInterface
    {
        private BaseHttpServer m_HttpServer;
        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionAdd += OnSceneAdded;
            /* prefer HTTPS over HTTP */
            if(!loader.TryGetHttpsServer(out m_HttpServer))
            {
                m_HttpServer = loader.HttpServer;
            }
            loader.XmlRpcServer.XmlRpcMethods.Add("getCurrencyQuote", HandleGetCurrencyQuote);
            loader.XmlRpcServer.XmlRpcMethods.Add("buyCurrency", HandleBuyCurrency);
        }

        private void OnSceneAdded(SceneInterface scene)
        {
            scene.SimulatorFeaturesExtrasMap["currency-base-uri"] = new AString(m_HttpServer.ServerURI);
        }

        public void GetGridInfo(Dictionary<string, string> dict)
        {
            dict["economy"] = m_HttpServer.ServerURI;
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

        private XmlRpc.XmlRpcResponse HandleGetCurrencyQuote(XmlRpc.XmlRpcRequest req)
        {
            Map structParam;
            UUID agentId;
            UUID secureSessionId;
            Integer currencyBuy;
            IValue language;
            if (!req.Params.TryGetValue(0, out structParam) ||
                !structParam.TryGetValue("agentId", out agentId) ||
                !structParam.TryGetValue("secureSessionId", out secureSessionId) ||
                !structParam.TryGetValue("currencyBuy", out currencyBuy) ||
                !structParam.TryGetValue("language", out language))
            {
                throw new XmlRpc.XmlRpcFaultException(4, "Missing parameters");
            }

            bool validated = false;
            IAgent agent = null;
            foreach (SceneInterface scene in m_Scenes.ValuesByKey1)
            {
                if (scene.Agents.TryGetValue(agentId, out agent) && agent.Session.SecureSessionID == secureSessionId)
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
                resdata.Add("errorURI", string.Empty);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }

            EconomyServiceInterface economyService = agent.EconomyService;

            if (economyService == null)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", this.GetLanguageString(agent.CurrentCulture, "NoEconomyConfigured", "No economy configured."));
                resdata.Add("errorURI", string.Empty);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }

            EconomyServiceInterface.CurrencyQuote quote;

            try
            {
                quote = economyService.GetCurrencyQuote(agent.Owner, language.ToString(), currencyBuy.AsInt);
            }
            catch (EconomyServiceInterface.UrlAttachedErrorException e)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", e.Message);
                resdata.Add("errorURI", e.Uri);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }
            catch (Exception e)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", "\n\n" + e.Message);
                resdata.Add("errorURI", string.Empty);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }

            resdata.Add("currency", quote.LocalCurrency);
            resdata.Add("estimatedCost", quote.EstimatedUsCents);
            resdata.Add("estimatedLocalCost", quote.EstimatedLocalCost);
            resdata.Add("currencyBuy", quote.CurrencyToBuy);
            switch (quote.ConfirmType)
            {
                case EconomyServiceInterface.ConfirmTypeEnum.None:
                    resdata.Add("confirm", "none");
                    break;

                default:
                    resdata.Add("confirm", "click");
                    break;

                case EconomyServiceInterface.ConfirmTypeEnum.Password:
                    resdata.Add("confirm", "password");
                    break;
            }

            return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
        }

        private XmlRpc.XmlRpcResponse HandleBuyCurrency(XmlRpc.XmlRpcRequest req)
        {
            Map structParam;
            UUID agentId;
            UUID secureSessionId;
            Integer currencyBuy;
            IValue language;
            IValue confirm;
            if (!req.Params.TryGetValue(0, out structParam) ||
                !structParam.TryGetValue("agentId", out agentId) ||
                !structParam.TryGetValue("secureSessionId", out secureSessionId) ||
                !structParam.TryGetValue("currencyBuy", out currencyBuy) ||
                !structParam.TryGetValue("language", out language) ||
                !structParam.TryGetValue("confirm", out confirm))
            {
                throw new XmlRpc.XmlRpcFaultException(4, "Missing parameters");
            }

            bool validated = false;
            IAgent agent = null;
            foreach (SceneInterface scene in m_Scenes.ValuesByKey1)
            {
                if (scene.Agents.TryGetValue(agentId, out agent) && agent.Session.SecureSessionID == secureSessionId)
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
                resdata.Add("errorURI", string.Empty);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }

            EconomyServiceInterface economyService = agent.EconomyService;

            if (economyService == null)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", this.GetLanguageString(agent.CurrentCulture, "NoEconomyConfigured", "No economy configured."));
                resdata.Add("errorURI", string.Empty);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }

            var quote = new EconomyServiceInterface.CurrencyBuy();
            IValue iv;
            if(structParam.TryGetValue("estimatedCost", out iv))
            {
                quote.EstimatedUsCents = iv.AsInt;
            }

            if(structParam.TryGetValue("estimatedLocalCost", out iv))
            {
                quote.EstimatedLocalCost = iv.ToString();
            }

            if(structParam.TryGetValue("password", out iv))
            {
                quote.Password = iv.ToString();
            }

            quote.CurrencyToBuy = currencyBuy.AsInt;

            switch(confirm.ToString())
            {
                case "click":
                    quote.ConfirmType = EconomyServiceInterface.ConfirmTypeEnum.Click;
                    break;

                case "password":
                    quote.ConfirmType = EconomyServiceInterface.ConfirmTypeEnum.Password;
                    break;

                default:
                    quote.ConfirmType = EconomyServiceInterface.ConfirmTypeEnum.None;
                    break;
            }

            try
            {
                economyService.BuyCurrency(agent.Owner, language.ToString(), quote);
            }
            catch(EconomyServiceInterface.UrlAttachedErrorException e)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", e.Message);
                resdata.Add("errorURI", e.Uri);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }
            catch (Exception e)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", "\n\n" + e.Message);
                resdata.Add("errorURI", string.Empty);
                return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
            }

            resdata.Add("success", true);
            return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
        }
    }
}
