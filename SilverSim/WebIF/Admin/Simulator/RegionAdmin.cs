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
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SilverSim.WebIF.Admin.Simulator
{
    #region Service implementation
    [Description("WebIF Region Admin Support")]
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
        AdminWebIF m_WebIF;

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
            m_WebIF = webif;
            webif.JsonMethods.Add("region.create", HandleCreate);
            webif.JsonMethods.Add("region.change", HandleChange);
            webif.JsonMethods.Add("region.delete", HandleDelete);
            webif.JsonMethods.Add("regions.list", HandleList);
            webif.JsonMethods.Add("region.start", HandleStart);
            webif.JsonMethods.Add("region.stop", HandleStop);
            webif.JsonMethods.Add("region.get", HandleGet);
            webif.JsonMethods.Add("region.get.estates", HandleGetEstates);
            webif.JsonMethods.Add("region.change.location", HandleChangeLocation);
            webif.JsonMethods.Add("region.change.estate", HandleChangeEstate);
            webif.JsonMethods.Add("region.change.access", HandleChangeAccess);
            webif.JsonMethods.Add("region.change.owner", HandleChangeOwner);
            webif.JsonMethods.Add("region.login.enable", HandleLoginEnable);
            webif.JsonMethods.Add("region.login.disable", HandleLoginDisable);
            webif.JsonMethods.Add("region.restart", HandleRestart);
            webif.JsonMethods.Add("region.restart.abort", HandleRestartAbort);
            webif.JsonMethods.Add("region.enable", HandleEnable);
            webif.JsonMethods.Add("region.disable", HandleDisable);
            webif.JsonMethods.Add("region.agent.notice", HandleAgentNotice);
            webif.JsonMethods.Add("region.notice", HandleNotice);
            webif.JsonMethods.Add("regions.notice", HandleNotices);
            webif.JsonMethods.Add("region.agents.list", HandleAgentsView);
            webif.JsonMethods.Add("region.agent.get", HandleAgentGet);
            webif.JsonMethods.Add("region.agent.kick", HandleAgentKick);
            webif.JsonMethods.Add("region.agent.teleporthome", HandleAgentTeleportHome);
            webif.JsonMethods.Add("region.environment.set", HandleEnvironmentSet);
            webif.JsonMethods.Add("region.environment.get", HandleEnvironmentGet);
            webif.JsonMethods.Add("region.environment.resettodefaults", HandleEnvironmentResetToDefaults);

            webif.AutoGrantRights["regions.manage"].Add("regions.view");
            webif.AutoGrantRights["regions.environmentcontrol"].Add("regions.view");
            webif.AutoGrantRights["regions.agents.kick"].Add("regions.view");
            webif.AutoGrantRights["regions.agents.kick"].Add("regions.agents.view");
            webif.AutoGrantRights["regions.agents.teleporthome"].Add("regions.view");
            webif.AutoGrantRights["regions.agents.teleporthome"].Add("regions.agents.view");
            webif.AutoGrantRights["regions.agents.notice"].Add("regions.view");
            webif.AutoGrantRights["regions.agents.notice"].Add("regions.agents.view");
            webif.AutoGrantRights["regions.agents.view"].Add("regions.view");
            webif.AutoGrantRights["regions.control"].Add("regions.view");
            webif.AutoGrantRights["regions.logincontrol"].Add("regions.view");
            webif.AutoGrantRights["region.notice"].Add("regions.view");

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

        #region Region View
        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleGetEstates(HttpRequest req, Map jsondata)
        {
            List<EstateInfo> estates = m_EstateService.All;

            Map res = new Map();
            AnArray estateRes = new AnArray();
            foreach (EstateInfo estate in estates)
            {
                Map m = new Map();
                m.Add("ID", estate.ID);
                m.Add("Name", estate.Name);
                estateRes.Add(m);
            }
            res.Add("estates", estateRes);
            AdminWebIF.SuccessResponse(req, res);
        }

        [AdminWebIF.RequiredRight("regions.view")]
        void HandleGet(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }
            RegionInfo rInfo;
            if(m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                rInfo.Owner = m_WebIF.ResolveName(rInfo.Owner);
                Map m = rInfo.ToJsonMap();
                GetRegionDetails(rInfo.ID, m);
                Map res = new Map();
                res.Add("region", m);
                uint estateID;
                EstateInfo estateInfo;
                if(m_EstateService.RegionMap.TryGetValue(rInfo.ID, out estateID) &&
                    m_EstateService.TryGetValue(estateID, out estateInfo))
                {
                    Map estateData = new Map();
                    estateData.Add("ID", estateInfo.ID);
                    estateData.Add("Name", estateInfo.Name);
                    res.Add("estate", estateData);
                }
                AdminWebIF.SuccessResponse(req, res);
            }
            else
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
        }

        void GetRegionDetails(UUID regionid, Map m)
        {
            SceneInterface scene;
            bool isOnline = SceneManager.Scenes.TryGetValue(regionid, out scene);
            m.Add("IsOnline", isOnline);
            m.Add("IsLoginsEnabled", isOnline && scene.LoginControl.IsLoginEnabled);
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
                region.Owner = m_WebIF.ResolveName(region.Owner);
                GetRegionDetails(region.ID, m);
                regionsRes.Add(m);
            }
            res.Add("regions", regionsRes);
            AdminWebIF.SuccessResponse(req, res);
        }
        #endregion

        #region Region Online Changes
        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleChangeAccess(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("access"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }

            RegionInfo rInfo;
            RegionAccess access;
            switch(jsondata["access"].ToString().ToLower())
            {
                case "pg":
                    access = RegionAccess.PG;
                    break;

                case "mature":
                    access = RegionAccess.Mature;
                    break;

                case "adult":
                    access = RegionAccess.Adult;
                    break;

                default:
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
            }

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
                return;
            }

            rInfo.Access = access;
            try
            {
                m_RegionStorage.RegisterRegion(rInfo);
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                return;
            }

            SceneInterface scene;
            if(SceneManager.Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.Access = access;
                scene.TriggerRegionDataChanged();
            }

            AdminWebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleChangeOwner(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("owner"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }

            RegionInfo rInfo;

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
                return;
            }

            if (!m_WebIF.TranslateToUUI(jsondata["owner"].ToString(), out rInfo.Owner))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                return;
            }

            try
            {
                m_RegionStorage.RegisterRegion(rInfo);
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                return;
            }

            SceneInterface scene;
            if (SceneManager.Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.Owner = rInfo.Owner;
            }

            AdminWebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleChangeLocation(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("location"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }

            RegionInfo rInfo;

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
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

            if(SceneManager.Scenes.ContainsKey(rInfo.ID))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.IsRunning);
                return;
            }

            try
            {
                m_RegionStorage.RegisterRegion(rInfo);
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                return;
            }

            SceneInterface scene;
            if (SceneManager.Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.Owner = rInfo.Owner;
            }

            AdminWebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIF.RequiredRight("regions.manage")]
        void HandleChangeEstate(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("estateid"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }

            RegionInfo rInfo;
            EstateInfo eInfo;
            if(!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo) ||
                !m_EstateService.TryGetValue(jsondata["estateid"].AsUInt, out eInfo))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
                return;
            }

            try
            {
                m_EstateService.RegionMap[rInfo.ID] = eInfo.ID;
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
                return;
            }
            SceneInterface scene;
            if(SceneManager.Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.TriggerEstateUpdate();
            }
            AdminWebIF.SuccessResponse(req, new Map());
        }
        #endregion

        #region Agents View and Control
        bool TryGetRootAgent(HttpRequest req, Map jsondata, out SceneInterface scene, out IAgent agent)
        {
            agent = null;
            scene = null;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("agentid"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotRunning);
            }
            else if (scene.RootAgents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                return true;
            }
            else
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            return false;
        }

        [AdminWebIF.RequiredRight("regions.agents.teleporthome")]
        void HandleAgentTeleportHome(HttpRequest req, Map jsondata)
        {
            IAgent agent;
            SceneInterface si;
            if(TryGetRootAgent(req, jsondata, out si, out agent))
            {
                string msg = this.GetLanguageString(agent.CurrentCulture, "YouHaveBeenKickedSinceYouCouldNotBeTeleportedHome", "You have been kicked since you could not be teleported home.");
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
        }

        [AdminWebIF.RequiredRight("regions.agents.kick")]
        void HandleAgentKick(HttpRequest req, Map jsondata)
        {
            IAgent agent;
            SceneInterface si;
            if (TryGetRootAgent(req, jsondata, out si, out agent))
            {
                string msg = this.GetLanguageString(agent.CurrentCulture, "YouHaveBeenKicked", "You have been kicked.");
                if(jsondata.ContainsKey("message"))
                {
                    msg = jsondata["message"].ToString();
                }
                agent.KickUser(msg);
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.agents.view")]
        void HandleAgentGet(HttpRequest req, Map jsondata)
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
            else if (!si.Agents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                Map res = new Map();
                res.Add("agent", agent.ToJsonMap(si));
                AdminWebIF.SuccessResponse(req, res);
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
                AdminWebIF.SuccessResponse(req, res);
            }
        }
        #endregion

        #region Manage regions
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
                else if (jsondata.ContainsKey("estateid"))
                {
                    if (!m_EstateService.TryGetValue(jsondata["estateid"].AsUInt, out selectedEstate))
                    {
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                    }
                    rInfo.Owner = selectedEstate.Owner;
                }

                if (jsondata.ContainsKey("owner") &&
                    !m_WebIF.TranslateToUUI(jsondata["owner"].ToString(), out rInfo.Owner))
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                    return;
                }
                if(jsondata.ContainsKey("status"))
                {
                    switch (jsondata["status"].ToString().ToLower())
                    {
                        case "enabled":
                            rInfo.Flags = RegionFlags.RegionOnline;
                            break;

                        case "disabled":
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
                    if(!m_WebIF.TranslateToUUI(jsondata["owner"].ToString(), out rInfo.Owner))
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
                try
                {
                    m_SimulationData.RemoveRegion(region.ID);
                    m_EstateService.RegionMap.Remove(region.ID);
                    m_RegionStorage.DeleteRegion(UUID.Zero, region.ID);
                }
                catch (Exception e)
                {
                    m_Log.ErrorFormat("Exception encountered when deleting region: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                }
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion

        #region Login Control
        [AdminWebIF.RequiredRight("regions.logincontrol")]
        void HandleLoginEnable(HttpRequest req, Map jsondata)
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
                scene.LoginControl.Ready(SceneInterface.ReadyFlags.LoginsEnable);
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.logincontrol")]
        void HandleLoginDisable(HttpRequest req, Map jsondata)
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
                scene.LoginControl.NotReady(SceneInterface.ReadyFlags.LoginsEnable);
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion

        #region Start/Stop Control
        [AdminWebIF.RequiredRight("regions.control")]
        void HandleRestart(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("seconds"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }

            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                scene.RequestRegionRestart(jsondata["seconds"].AsInt);
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.control")]
        void HandleRestartAbort(HttpRequest req, Map jsondata)
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
                scene.AbortRegionRestart();
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
        #endregion

        #region Notices
        [AdminWebIF.RequiredRight("region.notice")]
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

        [AdminWebIF.RequiredRight("region.agents.notice")]
        void HandleAgentNotice(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            IAgent agent;
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("agentid") ||
                !jsondata.ContainsKey("message"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene) ||
                !scene.RootAgents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            { 
                agent.SendRegionNotice(scene.Owner, jsondata["message"].ToString(), scene.ID);
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
        #endregion

        #region Enable/Disable
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
        #endregion

        #region Environment Control
        [AdminWebIF.RequiredRight("regions.environmentcontrol")]
        public void HandleEnvironmentSet(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("parameter") || !jsondata.ContainsKey("value"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                uint secperday;
                uint daysperyear;

                switch (jsondata["parameter"].ToString())
                {
                    case "UpdateTidalModelEveryMsecs":
                        scene.Environment.UpdateTidalModelEveryMsecs = jsondata["value"].AsInt;
                        break;

                    case "SunUpdateEveryMsecs":
                        scene.Environment.SunUpdateEveryMsecs = jsondata["value"].AsInt;
                        break;

                    case "SendSimTimeEveryNthSunUpdate":
                        scene.Environment.SendSimTimeEveryNthSunUpdate = jsondata["value"].AsUInt;
                        break;

                    case "UpdateWindModelEveryMsecs":
                        scene.Environment.UpdateWindModelEveryMsecs = jsondata["value"].AsInt;
                        break;

                    case "MoonPhaseOffset":
                        scene.Environment.MoonPhaseOffset = jsondata["value"].AsReal;
                        break;

                    case "MoonPeriodLengthInSecs":
                        scene.Environment.MoonPeriodLengthInSecs = jsondata["value"].AsReal;
                        break;

                    case "AverageSunTilt":
                        scene.Environment.AverageSunTilt = jsondata["value"].AsReal;
                        break;

                    case "SeasonalSunTilt":
                        scene.Environment.SeasonalSunTilt = jsondata["value"].AsReal;
                        break;

                    case "SunNormalizedOffset":
                        scene.Environment.SunNormalizedOffset = jsondata["value"].AsReal;
                        break;

                    case "SecondsPerDay":
                        scene.Environment.GetSunDurationParams(out secperday, out daysperyear);
                        secperday = jsondata["value"].AsUInt;
                        scene.Environment.SetSunDurationParams(secperday, daysperyear);
                        break;

                    case "DaysPerYear":
                        scene.Environment.GetSunDurationParams(out secperday, out daysperyear);
                        daysperyear = jsondata["value"].AsUInt;
                        scene.Environment.SetSunDurationParams(secperday, daysperyear);
                        break;

                    case "EnableTideControl":
                        scene.Environment[EnvironmentController.BooleanWaterParams.EnableTideControl] = jsondata["value"].AsBoolean;
                        break;

                    case "TidalBaseHeight":
                        scene.Environment[EnvironmentController.FloatWaterParams.TidalBaseHeight] = jsondata["value"].AsReal;
                        break;

                    case "TidalMoonAmplitude":
                        scene.Environment[EnvironmentController.FloatWaterParams.TidalMoonAmplitude] = jsondata["value"].AsReal;
                        break;

                    case "TidalSunAmplitude":
                        scene.Environment[EnvironmentController.FloatWaterParams.TidalSunAmplitude] = jsondata["value"].AsReal;
                        break;

                    case "EnableWeatherLightShare":
                        scene.Environment[EnvironmentController.BooleanWeatherParams.EnableLightShare] = jsondata["value"].AsBoolean;
                        break;

                    default:
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                }

                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.environmentcontrol")]
        public void HandleEnvironmentGet(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("parameter"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                Map res = new Map();
                uint secperday;
                uint daysperyear;

                switch(jsondata["parameter"].ToString())
                {
                    case "UpdateTidalModelEveryMsecs":
                        res.Add("value", scene.Environment.UpdateTidalModelEveryMsecs);
                        break;

                    case "SunUpdateEveryMsecs":
                        res.Add("value", scene.Environment.SunUpdateEveryMsecs);
                        break;

                    case "SendSimTimeEveryNthSunUpdate":
                        res.Add("value", (int)scene.Environment.SendSimTimeEveryNthSunUpdate);
                        break;

                    case "UpdateWindModelEveryMsecs":
                        res.Add("value", scene.Environment.UpdateWindModelEveryMsecs);
                        break;

                    case "MoonPhaseOffset":
                        res.Add("value", scene.Environment.MoonPhaseOffset);
                        break;

                    case "MoonPeriodLengthInSecs":
                        res.Add("value", scene.Environment.MoonPeriodLengthInSecs);
                        break;

                    case "AverageSunTilt":
                        res.Add("value", scene.Environment.AverageSunTilt);
                        break;

                    case "SeasonalSunTilt":
                        res.Add("value", scene.Environment.SeasonalSunTilt);
                        break;

                    case "SunNormalizedOffset":
                        res.Add("value", scene.Environment.SunNormalizedOffset);
                        break;

                    case "SecondsPerDay":
                        scene.Environment.GetSunDurationParams(out secperday, out daysperyear);
                        res.Add("value", (int)secperday);
                        break;

                    case "DaysPerYear":
                        scene.Environment.GetSunDurationParams(out secperday, out daysperyear);
                        res.Add("value", (int)daysperyear);
                        break;

                    case "EnableTideControl":
                        res.Add("value", scene.Environment[EnvironmentController.BooleanWaterParams.EnableTideControl]);
                        break;

                    case "TidalBaseHeight":
                        res.Add("value", scene.Environment[EnvironmentController.FloatWaterParams.TidalBaseHeight]);
                        break;

                    case "TidalMoonAmplitude":
                        res.Add("value", scene.Environment[EnvironmentController.FloatWaterParams.TidalMoonAmplitude]);
                        break;

                    case "TidalSunAmplitude":
                        res.Add("value", scene.Environment[EnvironmentController.FloatWaterParams.TidalSunAmplitude]);
                        break;

                    case "EnableWeatherLightShare":
                        res.Add("value", scene.Environment[EnvironmentController.BooleanWeatherParams.EnableLightShare]);
                        break;

                    default:
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                }
                AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIF.RequiredRight("regions.environmentcontrol")]
        public void HandleEnvironmentResetToDefaults(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("which"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                switch (jsondata["which"].ToString())
                {
                    case "all":
                        scene.Environment.ResetToDefaults();
                        break;

                    case "sun":
                        scene.Environment.ResetSunToDefaults();
                        break;

                    case "moon":
                        scene.Environment.ResetMoonToDefaults();
                        break;

                    case "tidal":
                        scene.Environment.ResetTidalToDefaults();
                        break;

                    case "wind":
                        scene.Environment.ResetWindToDefaults();
                        break;

                    default:
                        AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidParameter);
                        return;
                }

                AdminWebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion
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