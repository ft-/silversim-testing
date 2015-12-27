// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.WebIF.Admin.Simulator
{
    #region Service implementation
    public class RegionAdmin : IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ADMIN WEB IF - REGION");

        readonly string m_RegionStorageName;
        readonly string m_SimulationDataName;
        readonly string m_EstateServiceName;
        GridServiceInterface m_RegionStorage;
        SceneFactoryInterface m_SceneFactory;
        EstateServiceInterface m_EstateService;
        SimulationDataStorageInterface m_SimulationData;
        readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();
        uint m_HttpPort;
        string m_ExternalHostName = string.Empty;
        string m_Scheme = Uri.UriSchemeHttp;

        public RegionAdmin(string regionStorageName, string simulationDataName, string estateServiceName)
        {
            m_RegionStorageName = regionStorageName;
            m_SimulationDataName = simulationDataName;
            m_EstateServiceName = estateServiceName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs["Network"];
            if (config != null)
            {
                m_ExternalHostName = config.GetString("ExternalHostName", "SYSTEMIP");
                m_HttpPort = (uint)config.GetInt("HttpListenerPort", 9000);

                if (config.Contains("ServerCertificate"))
                {
                    m_Scheme = Uri.UriSchemeHttps;
                }
            }

            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_SimulationData = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataName);
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);

            AdminWebIF webif = loader.GetAdminWebIF();
            webif.JsonMethods.Add("region.create", HandleCreate);
            webif.JsonMethods.Add("region.change", HandleChange);
            webif.JsonMethods.Add("region.delete", HandleDelete);
            webif.JsonMethods.Add("regions.list", HandleList);
            webif.JsonMethods.Add("region.start", HandleStart);
            webif.JsonMethods.Add("region.stop", HandleStop);
            webif.JsonMethods.Add("region.enable", HandleEnable);
            webif.JsonMethods.Add("region.disable", HandleDisable);
            webif.JsonMethods.Add("region.notice", HandleNotice);
            webif.JsonMethods.Add("regions.notice", HandleNotices);
            webif.JsonMethods.Add("region.agents.view", HandleAgentsView);
            webif.JsonMethods.Add("region.agents.kick", HandleAgentKick);

            IConfig sceneConfig = loader.Config.Configs["DefaultSceneImplementation"];
            if (null != sceneConfig)
            {
                string avatarNameServices = sceneConfig.GetString("AvatarNameServices", string.Empty);
                if (!string.IsNullOrEmpty(avatarNameServices))
                {
                    foreach (string p in avatarNameServices.Split(','))
                    {
                        m_AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(p.Trim()));
                    }
                }
            }

        }

        [AdminWebIF.RequiredRight("regions.view")]
        void HandleList(HttpRequest req, Map jsondata)
        {
            List<RegionInfo> regions = m_RegionStorage.GetAllRegions(UUID.Zero);

            Map res = new Map();
            AnArray regionsRes = new AnArray();
            foreach (RegionInfo region in regions)
            {
                Map m = region.ToJsonMap();
                region.Owner = ResolveName(region.Owner);
                m.Add("IsOnline", SceneManager.Scenes.ContainsKey(region.ID));
                regionsRes.Add(m);
            }
            res.Add("regions", regionsRes);
            AdminWebIF.SuccessResponse(req, res);
        }

        [AdminWebIF.RequiredRight("regions.agents.teleporthome")]
        void HandleAgentTeleportHome(HttpRequest req, Map jsondata)
        {
            SceneInterface si;
            IAgent agent;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("agentid"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out si))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotRunning);
            }
            else if (si.RootAgents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                string msg = "You have been kicked since you could not be teleported home.";
                if (jsondata.ContainsKey("message"))
                {
                    msg = jsondata["message"].ToString();
                }
                if(!agent.TeleportHome(si))
                {
                    agent.KickUser(msg);
                }
                AdminWebIF.SuccessResponse(req, new Map());
            }
            else
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
        }

        [AdminWebIF.RequiredRight("regions.agents.kick")]
        void HandleAgentKick(HttpRequest req, Map jsondata)
        {
            SceneInterface si;
            IAgent agent;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("agentid"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out si))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotRunning);
            }
            else if(si.RootAgents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                string msg = "You have been kicked.";
                if(jsondata.ContainsKey("message"))
                {
                    msg = jsondata["message"].ToString();
                }
                agent.KickUser(msg);
                AdminWebIF.SuccessResponse(req, new Map());
            }
            else
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
        }

        [AdminWebIF.RequiredRight("regions.agents.view")]
        void HandleAgentsView(HttpRequest req, Map jsondata)
        {
            SceneInterface si;
            if (!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out si))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotRunning);
            }
            else
            {
                bool rootOnly = jsondata.ContainsKey("root_only");
                bool userOnly = jsondata.ContainsKey("no_npc");
                Map res = new Map();
                AnArray agents = new AnArray();
                foreach(IAgent agent in si.Agents)
                {
                    if(userOnly && agent.IsNpc)
                    {
                        continue;
                    }
                    if(rootOnly && !agent.IsInScene(si))
                    {
                        continue;
                    }
                    agents.Add(agent.ToJsonMap(si));
                }
                res.Add("agents", agents);
            }
        }

        UUI ResolveName(UUI uui)
        {
            UUI resultUui;
            foreach (AvatarNameServiceInterface service in m_AvatarNameServices)
            {
                if (service.TryGetValue(uui, out resultUui))
                {
                    return resultUui;
                }
            }
            return uui;
        }

        bool TranslateToUUI(string arg, out UUI uui)
        {
            uui = UUI.Unknown;
            if (arg.Contains(","))
            {
                bool found = false;
                string[] names = arg.Split(new char[] { ',' }, 2);
                if (names.Length == 1)
                {
                    names = new string[] { names[0], string.Empty };
                }
                foreach (AvatarNameServiceInterface service in m_AvatarNameServices)
                {
                    UUI founduui;
                    if (service.TryGetValue(names[0], names[1], out founduui))
                    {
                        uui = founduui;
                        found = true;
                        break;
                    }
                }
                return found;
            }
            else if (UUID.TryParse(arg, out uui.ID))
            {
                bool found = false;
                foreach (AvatarNameServiceInterface service in m_AvatarNameServices)
                {
                    UUI founduui;
                    if (service.TryGetValue(uui.ID, out founduui))
                    {
                        uui = founduui;
                        found = true;
                        break;
                    }
                }
                return found;
            }
            else if (!UUI.TryParse(arg, out uui))
            {
                return false;
            }
            return true;
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleCreate(HttpRequest req, Map jsondata)
        {
            RegionInfo rInfo;
            if (!jsondata.ContainsKey("name") || !jsondata.ContainsKey("port") || !jsondata.ContainsKey("location"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (m_EstateService.All.Count == 0)
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NoEstates);
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, jsondata["name"].ToString(), out rInfo))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.AlreadyExists);
            }
            else
            {
                rInfo = new RegionInfo();
                EstateInfo selectedEstate = null;
                rInfo.Name = jsondata["name"].ToString();
                rInfo.ID = UUID.Random;
                rInfo.Access = RegionAccess.Mature;
                rInfo.ServerHttpPort = m_HttpPort;
                rInfo.ScopeID = UUID.Zero;
                rInfo.ServerIP = m_ExternalHostName;
                rInfo.Size = new GridVector(256, 256);
                rInfo.ProductName = "Mainland";

                if (!uint.TryParse(jsondata["port"].ToString(), out rInfo.ServerPort))
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }
                if (rInfo.ServerPort < 1 || rInfo.ServerPort > 65535)
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }

                try
                {
                    rInfo.Location = new GridVector(jsondata["location"].ToString(), 256);
                }
                catch
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }

                if(jsondata.ContainsKey("externalhostname"))
                {
                    rInfo.ServerIP = jsondata["externalhostname"].ToString();
                }
                if(jsondata.ContainsKey("regionid") &&
                    !UUID.TryParse(jsondata["regionid"].ToString(), out rInfo.ID))
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }
                if (jsondata.ContainsKey("scopeid") &&
                    !UUID.TryParse(jsondata["scopeid"].ToString(), out rInfo.ScopeID))
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }
                if (jsondata.ContainsKey("staticmaptile") &&
                    !UUID.TryParse(jsondata["staticmaptile"].ToString(), out rInfo.RegionMapTexture))
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }
                if(jsondata.ContainsKey("size"))
                {
                    try
                    {
                        rInfo.Size = new GridVector(jsondata["size"].ToString(), 256);
                    }
                    catch
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                }
                if(jsondata.ContainsKey("productname"))
                {
                    rInfo.ProductName = jsondata["productname"].ToString();
                }
                if(jsondata.ContainsKey("estate"))
                {
                    if (!m_EstateService.TryGetValue(jsondata["estate"].ToString(), out selectedEstate))
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                    rInfo.Owner = selectedEstate.Owner;
                }

                if(jsondata.ContainsKey("owner") &&
                    !TranslateToUUI(jsondata["owner"].ToString(), out rInfo.Owner))
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }
                if(jsondata.ContainsKey("status"))
                {
                    switch (jsondata["status"].ToString().ToLower())
                    {
                        case "online":
                            rInfo.Flags = RegionFlags.RegionOnline;
                            break;

                        case "offline":
                            rInfo.Flags = RegionFlags.None;
                            break;

                        default:
                            AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                            return;
                    }
                }
                if(jsondata.ContainsKey("access"))
                {
                    switch (jsondata["access"].ToString().ToLower())
                    {
                        case "trial":
                            rInfo.Access = RegionAccess.Trial;
                            break;

                        case "pg":
                            rInfo.Access = RegionAccess.PG;
                            break;

                        case "mature":
                            rInfo.Access = RegionAccess.Mature;
                            break;

                        case "adult":
                            rInfo.Access = RegionAccess.Adult;
                            break;

                        default:
                            AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                            return;
                    }
                }
                rInfo.ServerURI = string.Format("{0}://{1}:{2}/", m_Scheme, m_ExternalHostName, m_HttpPort);
                try
                {
                    m_RegionStorage.RegisterRegion(rInfo);
                }
                catch
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                    return;
                }

                if (selectedEstate != null)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                }
                else
                {
                    List<EstateInfo> allEstates = m_EstateService.All;
                    List<EstateInfo> ownerEstates = new List<EstateInfo>(from estate in allEstates where estate.Owner.EqualsGrid(rInfo.Owner) select estate);
                    if (ownerEstates.Count != 0)
                    {
                        m_EstateService.RegionMap[rInfo.ID] = ownerEstates[0].ID;
                    }
                    else if (allEstates.Count != 0)
                    {
                        m_EstateService.RegionMap[rInfo.ID] = allEstates[0].ID;
                    }
                }
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleChange(HttpRequest req, Map jsondata)
        {
            RegionInfo rInfo;
            if(!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(UUID.Zero, jsondata["id"].AsUUID, out rInfo))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                bool changeRegionData = false;
                EstateInfo selectedEstate = null;
                if(jsondata.ContainsKey("name"))
                {
                    rInfo.Name = jsondata["name"].ToString();
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("port"))
                {
                    rInfo.ServerPort = jsondata["port"].AsUInt;
                    if(rInfo.ServerPort < 1 || rInfo.ServerPort > 65535)
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("scopeid"))
                {
                    if (!UUID.TryParse(jsondata["scopeid"].ToString(), out rInfo.ScopeID))
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("productname"))
                {
                    rInfo.ProductName = jsondata["productname"].ToString();
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("owner"))
                {
                    if(!TranslateToUUI(jsondata["owner"].ToString(), out rInfo.Owner))
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("estate"))
                {
                    if(!m_EstateService.TryGetValue(jsondata["estate"].ToString(), out selectedEstate))
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("externalhostname"))
                {
                    rInfo.ServerIP = jsondata["externalhostname"].ToString();
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("access"))
                {
                    switch (jsondata["access"].ToString().ToLower())
                    {
                        case "trial":
                            rInfo.Access = RegionAccess.Trial;
                            break;

                        case "pg":
                            rInfo.Access = RegionAccess.PG;
                            break;

                        case "mature":
                            rInfo.Access = RegionAccess.Mature;
                            break;

                        case "adult":
                            rInfo.Access = RegionAccess.Adult;
                            break;

                        default:
                            AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                            return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("staticmaptile"))
                {
                    if(!UUID.TryParse(jsondata["staticmaptile"].ToString(), out rInfo.RegionMapTexture))
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }

                SceneInterface si;
                if (SceneManager.Scenes.TryGetValue(rInfo.ID, out si))
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.IsRunning);
                    return;
                }
                if (changeRegionData)
                {
                    try
                    {
                        m_RegionStorage.RegisterRegion(rInfo);
                    }
                    catch
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                        return;
                    }
                }
                if (null != selectedEstate)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                }
            }
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleDelete(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else if (SceneManager.Scenes.ContainsKey(region.ID))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.IsRunning);
            }
            else
            {
                m_RegionStorage.DeleteRegion(UUID.Zero, region.ID);
                m_SimulationData.RemoveRegion(region.ID);
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.control")]
        void HandleStart(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else if (SceneManager.Scenes.ContainsKey(region.ID))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.AlreadyStarted);
            }
            else
            {
                SceneInterface si;
                m_Log.InfoFormat("Starting Region {0} ({1})", region.Name, region.ID.ToString());
                try
                {
                    si = m_SceneFactory.Instantiate(region);
                }
                catch (Exception e)
                {
                    m_Log.ErrorFormat("Failed to start region: {0}", e.Message);
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.FailedToStart);
                    return;
                }
                SceneManager.Scenes.Add(si);
                si.LoadSceneAsync();
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.control")]
        void HandleStop(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }

            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                SceneManager.Scenes.Remove(scene);
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.notice")]
        void HandleNotice(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("message"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                foreach(IAgent agent in scene.RootAgents)
                {
                    agent.SendRegionNotice(scene.Owner, jsondata["message"].ToString(), scene.ID);
                }
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.notice")]
        void HandleNotices(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("message"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else
            {
                foreach (SceneInterface scene in SceneManager.Scenes.Values)
                {
                    foreach (IAgent agent in scene.RootAgents)
                    {
                        agent.SendRegionNotice(scene.Owner, jsondata["message"].ToString(), scene.ID);
                    }
                }
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleEnable(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                try
                {
                    m_RegionStorage.AddRegionFlags(region.ID, RegionFlags.RegionOnline);
                    AdminWebIF.SuccessResponse(req, new Map());
                }
                catch
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                }
            }
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleDisable(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                try
                {
                    m_RegionStorage.RemoveRegionFlags(region.ID, RegionFlags.RegionOnline);
                    AdminWebIF.SuccessResponse(req, new Map());
                }
                catch
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("RegionAdmin")]
    public class RegionAdminFactory : IPluginFactory
    {
        public RegionAdminFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new RegionAdmin(
                ownSection.GetString("RegionStorage", "RegionStorage"),
                ownSection.GetString("SimulationDataStorage", "SimulationDataStorage"),
                ownSection.GetString("EstateService", "EstateService"));
        }
    }
    #endregion
}