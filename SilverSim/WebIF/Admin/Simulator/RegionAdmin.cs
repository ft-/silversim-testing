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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

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
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.ServerParam;
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

        private readonly string m_RegionStorageName;
        private readonly string m_SimulationDataName;
        private readonly string m_EstateServiceName;
        private GridServiceInterface m_RegionStorage;
        private SceneFactoryInterface m_SceneFactory;
        private EstateServiceInterface m_EstateService;
        private ServerParamServiceInterface m_ServerParams;
        private SimulationDataStorageInterface m_SimulationData;
        private BaseHttpServer m_HttpServer;
        private IAdminWebIF m_WebIF;
        private SceneList m_Scenes;
        private ConfigurationLoader m_Loader;

        public RegionAdmin(string regionStorageName, string simulationDataName, string estateServiceName)
        {
            m_RegionStorageName = regionStorageName;
            m_SimulationDataName = simulationDataName;
            m_EstateServiceName = estateServiceName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Loader = loader;
            m_HttpServer = loader.HttpServer;
            m_Scenes = loader.Scenes;

            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_SimulationData = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataName);
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            m_ServerParams = loader.GetServerParamStorage();

            IAdminWebIF webif = loader.GetAdminWebIF();
            m_WebIF = webif;
            webif.ModuleNames.Add("regions");
            webif.JsonMethods.Add("region.create", HandleCreate);
            webif.JsonMethods.Add("region.change", HandleChange);
            webif.JsonMethods.Add("region.delete", HandleDelete);
            webif.JsonMethods.Add("regions.list", HandleList);
            webif.JsonMethods.Add("region.start", HandleStart);
            webif.JsonMethods.Add("region.stop", HandleStop);
            webif.JsonMethods.Add("region.getports", HandleGetPorts);
            webif.JsonMethods.Add("region.get", HandleGet);
            webif.JsonMethods.Add("region.get.estates", HandleGetEstates);
            webif.JsonMethods.Add("region.change.location", HandleChangeLocation);
            webif.JsonMethods.Add("region.change.estate", HandleChangeEstate);
            webif.JsonMethods.Add("region.change.access", HandleChangeAccess);
            webif.JsonMethods.Add("region.change.owner", HandleChangeOwner);
            webif.JsonMethods.Add("region.simconsole.regionowner", HandleEnableRegionOwnerSimConsole);
            webif.JsonMethods.Add("region.simconsole.estatemanager", HandleEnableEstateManagerSimConsole);
            webif.JsonMethods.Add("region.simconsole.estateowner", HandleEnableEstateOwnerSimConsole);
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
            webif.JsonMethods.Add("scriptengines.list", HandleScriptEnginesList);

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
            webif.AutoGrantRights["regions.manage"].Add("scriptengines.view");
            webif.AutoGrantRights["regions.view"].Add("scriptengines.view");
        }

        #region Script Engines
        [AdminWebIfRequiredRight("scriptengines.view")]
        private void HandleScriptEnginesList(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
            var compilerRes = new AnArray();
            foreach(string name in CompilerRegistry.ScriptCompilers.Names)
            {
                var compiler = CompilerRegistry.ScriptCompilers[name];
                var m = new Map();
                ScriptEngineNameAttribute attr;
                Type compilerType = compiler.GetType();
                attr = Attribute.GetCustomAttribute(compilerType, typeof(ScriptEngineNameAttribute)) as ScriptEngineNameAttribute;
                if(attr == null || attr.Name != name)
                {
                    continue;
                }
                var descAttr = Attribute.GetCustomAttribute(compilerType, typeof(DescriptionAttribute)) as DescriptionAttribute;
                var desc = descAttr != null ? descAttr.Description : string.Empty;
                m.Add("name", name);
                m.Add("description", desc);
                compilerRes.Add(m);
            }
            res.Add("scriptengines", compilerRes);
            m_WebIF.SuccessResponse(req, res);
        }
        #endregion

        #region Region View
        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleGetEstates(HttpRequest req, Map jsondata)
        {
            var estates = m_EstateService.All;

            var res = new Map();
            var estateRes = new AnArray();
            foreach (var estate in estates)
            {
                estateRes.Add(new Map
                {
                    { "ID", estate.ID },
                    { "Name", estate.Name }
                });
            }
            res.Add("estates", estateRes);
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("regions.view")]
        private void HandleGetPorts(HttpRequest req, Map jsondata)
        {
            var resdata = new Map();
            var tcpdata = new Map();
            resdata.Add("tcp", tcpdata);
            foreach(var kvp in m_Loader.KnownTcpPorts)
            {
                tcpdata.Add(kvp.Key.ToString(), kvp.Value);
            }
            var udpdata = new Map();
            resdata.Add("udp", udpdata);
            foreach (RegionInfo region in m_RegionStorage.GetAllRegions(UUID.Zero))
            {
                var regiondata = new Map
                {
                    { "type", "regionport" },
                    { "online", m_Scenes.ContainsKey(region.ID) },
                    { "regionid", region.ID },
                    { "regionname", region.Name }
                };
                udpdata.Add(region.ServerPort.ToString(), regiondata);
            }
            m_WebIF.SuccessResponse(req, resdata);
        }

        [AdminWebIfRequiredRight("regions.view")]
        private void HandleGet(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            RegionInfo rInfo;
            if(m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                rInfo.Owner = m_WebIF.ResolveName(rInfo.Owner);
                Map m = rInfo.ToJsonMap();
                GetRegionDetails(rInfo.ID, m);
                var res = new Map
                {
                    ["region"] = m
                };
                uint estateID;
                EstateInfo estateInfo;
                if(m_EstateService.RegionMap.TryGetValue(rInfo.ID, out estateID) &&
                    m_EstateService.TryGetValue(estateID, out estateInfo))
                {
                    var estateData = new Map
                    {
                        { "ID", estateInfo.ID },
                        { "Name", estateInfo.Name }
                    };
                    res.Add("estate", estateData);
                }

                var permissions = new Map();
                m = new Map
                {
                    { "effective", m_ServerParams.GetBoolean(rInfo.ID, RegionOwnerIsSimConsoleUser, false) },
                    { "global", m_ServerParams.GetBoolean(UUID.Zero, RegionOwnerIsSimConsoleUser, false) }
                };
                permissions.Add("region_owner", m);
                m = new Map
                {
                    { "effective", m_ServerParams.GetBoolean(rInfo.ID, EstateOwnerIsSimConsoleUser, false) },
                    { "global", m_ServerParams.GetBoolean(UUID.Zero, EstateOwnerIsSimConsoleUser, false) }
                };
                permissions.Add("estate_owner", m);
                m = new Map
                {
                    { "effective", m_ServerParams.GetBoolean(rInfo.ID, EstateManagerIsSimConsoleUser, false) },
                    { "global", m_ServerParams.GetBoolean(UUID.Zero, EstateManagerIsSimConsoleUser, false) }
                };
                permissions.Add("estate_manager", m);
                res.Add("simconsole", permissions);

                m_WebIF.SuccessResponse(req, res);
            }
            else
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
        }

        private void GetRegionDetails(UUID regionid, Map m)
        {
            SceneInterface scene;
            var isOnline = m_Scenes.TryGetValue(regionid, out scene);
            m.Add("IsOnline", isOnline);
            m.Add("IsLoginsEnabled", isOnline && scene.LoginControl.IsLoginEnabled);
        }

        [AdminWebIfRequiredRight("regions.view")]
        private void HandleList(HttpRequest req, Map jsondata)
        {
            var regions = m_RegionStorage.GetAllRegions(UUID.Zero);

            var res = new Map();
            var regionsRes = new AnArray();
            foreach (var region in regions)
            {
                var m = region.ToJsonMap();
                region.Owner = m_WebIF.ResolveName(region.Owner);
                GetRegionDetails(region.ID, m);
                regionsRes.Add(m);
            }
            res.Add("regions", regionsRes);
            m_WebIF.SuccessResponse(req, res);
        }
        #endregion

        #region Region Online Changes
        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleChangeAccess(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("access"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
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
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
            }

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            rInfo.Access = access;
            try
            {
                m_RegionStorage.RegisterRegion(rInfo);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }

            SceneInterface scene;
            if(m_Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.Access = access;
                scene.TriggerRegionDataChanged();
            }

            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleChangeOwner(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("owner"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            RegionInfo rInfo;

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            if (!m_WebIF.TranslateToUUI(jsondata["owner"].ToString(), out rInfo.Owner))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                return;
            }

            try
            {
                m_RegionStorage.RegisterRegion(rInfo);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }

            SceneInterface scene;
            if (m_Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.Owner = rInfo.Owner;
            }

            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleChangeLocation(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("location"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            RegionInfo rInfo;

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            try
            {
                rInfo.Location = new GridVector(jsondata["location"].ToString(), 256);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                return;
            }

            if(m_Scenes.ContainsKey(rInfo.ID))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.IsRunning);
                return;
            }

            try
            {
                m_RegionStorage.RegisterRegion(rInfo);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }

            SceneInterface scene;
            if (m_Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.Owner = rInfo.Owner;
            }

            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleChangeEstate(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("estateid"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            RegionInfo rInfo;
            EstateInfo eInfo;
            if(!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out rInfo) ||
                !m_EstateService.TryGetValue(jsondata["estateid"].AsUInt, out eInfo))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            try
            {
                m_EstateService.RegionMap[rInfo.ID] = eInfo.ID;
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            SceneInterface scene;
            if(m_Scenes.TryGetValue(rInfo.ID, out scene))
            {
                scene.TriggerEstateUpdate();
            }
            m_WebIF.SuccessResponse(req, new Map());
        }
        #endregion

        #region Agents View and Control
        private bool TryGetRootAgent(HttpRequest req, Map jsondata, out SceneInterface scene, out IAgent agent)
        {
            agent = null;
            scene = null;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("agentid"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotRunning);
            }
            else if (scene.RootAgents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                return true;
            }
            else
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            return false;
        }

        [AdminWebIfRequiredRight("regions.agents.teleporthome")]
        private void HandleAgentTeleportHome(HttpRequest req, Map jsondata)
        {
            IAgent agent;
            SceneInterface si;
            if(TryGetRootAgent(req, jsondata, out si, out agent))
            {
                var msg = this.GetLanguageString(agent.CurrentCulture, "YouHaveBeenKickedSinceYouCouldNotBeTeleportedHome", "You have been kicked since you could not be teleported home.");
                if (jsondata.ContainsKey("message"))
                {
                    msg = jsondata["message"].ToString();
                }
                if(!agent.TeleportHome(si))
                {
                    agent.KickUser(msg);
                }
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.agents.kick")]
        private void HandleAgentKick(HttpRequest req, Map jsondata)
        {
            IAgent agent;
            SceneInterface si;
            if (TryGetRootAgent(req, jsondata, out si, out agent))
            {
                var msg = this.GetLanguageString(agent.CurrentCulture, "YouHaveBeenKicked", "You have been kicked.");
                if(jsondata.ContainsKey("message"))
                {
                    msg = jsondata["message"].ToString();
                }
                agent.KickUser(msg);
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.agents.view")]
        private void HandleAgentGet(HttpRequest req, Map jsondata)
        {
            SceneInterface si;
            IAgent agent;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("agentid"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out si))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotRunning);
            }
            else if (!si.Agents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                var res = new Map
                {
                    ["agent"] = agent.ToJsonMap(si)
                };
                m_WebIF.SuccessResponse(req, res);
            }
        }

        [AdminWebIfRequiredRight("regions.agents.view")]
        private void HandleAgentsView(HttpRequest req, Map jsondata)
        {
            SceneInterface si;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out si))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotRunning);
            }
            else
            {
                var rootOnly = jsondata.ContainsKey("root_only");
                var userOnly = jsondata.ContainsKey("no_npc");
                var res = new Map();
                var agents = new AnArray();
                foreach(var agent in si.Agents)
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
                m_WebIF.SuccessResponse(req, res);
            }
        }
        #endregion

        #region Manage regions
        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleCreate(HttpRequest req, Map jsondata)
        {
            RegionInfo rInfo;
            if (!jsondata.ContainsKey("name") || !jsondata.ContainsKey("port") || !jsondata.ContainsKey("location"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (m_EstateService.All.Count == 0)
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NoEstates);
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, jsondata["name"].ToString(), out rInfo))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.AlreadyExists);
            }
            else
            {
                rInfo = new RegionInfo();
                EstateInfo selectedEstate = null;
                rInfo.Name = jsondata["name"].ToString();
                rInfo.ID = UUID.Random;
                rInfo.Access = RegionAccess.Mature;
                rInfo.ServerHttpPort = m_HttpServer.Port;
                rInfo.ScopeID = UUID.Zero;
                rInfo.ServerIP = string.Empty;
                rInfo.Size = new GridVector(256, 256);
                rInfo.ProductName = "Mainland";

                if (Uri.IsWellFormedUriString(rInfo.Name, UriKind.Absolute))
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
                if (!uint.TryParse(jsondata["port"].ToString(), out rInfo.ServerPort))
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
                if (rInfo.ServerPort < 1 || rInfo.ServerPort > 65535)
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }

                try
                {
                    rInfo.Location = new GridVector(jsondata["location"].ToString(), 256);
                }
                catch
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }

                if(jsondata.ContainsKey("externalhostname"))
                {
                    rInfo.ServerIP = jsondata["externalhostname"].ToString();
                }
                if(jsondata.ContainsKey("regionid") &&
                    !UUID.TryParse(jsondata["regionid"].ToString(), out rInfo.ID))
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
                if (jsondata.ContainsKey("scopeid") &&
                    !UUID.TryParse(jsondata["scopeid"].ToString(), out rInfo.ScopeID))
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
                if (jsondata.ContainsKey("staticmaptile") &&
                    !UUID.TryParse(jsondata["staticmaptile"].ToString(), out rInfo.RegionMapTexture))
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
                if(jsondata.ContainsKey("size"))
                {
                    try
                    {
                        rInfo.Size = new GridVector(jsondata["size"].ToString(), 1);
                    }
                    catch
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    if (rInfo.Size.X % 256 != 0 || rInfo.Size.Y % 256 != 0)
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                }
                if (jsondata.ContainsKey("productname"))
                {
                    rInfo.ProductName = jsondata["productname"].ToString();
                }
                if(jsondata.ContainsKey("estate"))
                {
                    if (!m_EstateService.TryGetValue(jsondata["estate"].ToString(), out selectedEstate))
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    rInfo.Owner = selectedEstate.Owner;
                }
                else if (jsondata.ContainsKey("estateid"))
                {
                    if (!m_EstateService.TryGetValue(jsondata["estateid"].AsUInt, out selectedEstate))
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    rInfo.Owner = selectedEstate.Owner;
                }

                if (jsondata.ContainsKey("owner") &&
                    !m_WebIF.TranslateToUUI(jsondata["owner"].ToString(), out rInfo.Owner))
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
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
                            m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
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
                            m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                            return;
                    }
                }
                rInfo.ServerURI = m_HttpServer.ServerURI;
                try
                {
                    m_RegionStorage.RegisterRegion(rInfo);
                }
                catch
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                    return;
                }

                if (selectedEstate != null)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                }
                else
                {
                    var allEstates = m_EstateService.All;
                    var ownerEstates = new List<EstateInfo>(from estate in allEstates where estate.Owner.EqualsGrid(rInfo.Owner) select estate);
                    if (ownerEstates.Count != 0)
                    {
                        m_EstateService.RegionMap[rInfo.ID] = ownerEstates[0].ID;
                    }
                    else if (allEstates.Count != 0)
                    {
                        m_EstateService.RegionMap[rInfo.ID] = allEstates[0].ID;
                    }
                }
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleChange(HttpRequest req, Map jsondata)
        {
            RegionInfo rInfo;
            if(!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(UUID.Zero, jsondata["id"].AsUUID, out rInfo))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                bool changeRegionData = false;
                EstateInfo selectedEstate = null;
                if(jsondata.ContainsKey("name"))
                {
                    rInfo.Name = jsondata["name"].ToString();
                    if (Uri.IsWellFormedUriString(rInfo.Name, UriKind.Absolute))
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("port"))
                {
                    rInfo.ServerPort = jsondata["port"].AsUInt;
                    if(rInfo.ServerPort < 1 || rInfo.ServerPort > 65535)
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("scopeid"))
                {
                    if (!UUID.TryParse(jsondata["scopeid"].ToString(), out rInfo.ScopeID))
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
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
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("estate"))
                {
                    if(!m_EstateService.TryGetValue(jsondata["estate"].ToString(), out selectedEstate))
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
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
                            m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                            return;
                    }
                    changeRegionData = true;
                }
                if(jsondata.ContainsKey("staticmaptile"))
                {
                    if(!UUID.TryParse(jsondata["staticmaptile"].ToString(), out rInfo.RegionMapTexture))
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    changeRegionData = true;
                }

                SceneInterface si;
                if (m_Scenes.TryGetValue(rInfo.ID, out si))
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.IsRunning);
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
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                        return;
                    }
                }
                if (selectedEstate != null)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                }
            }
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleDelete(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else if (m_Scenes.ContainsKey(region.ID))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.IsRunning);
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
                m_WebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion

        #region Login Control
        [AdminWebIfRequiredRight("regions.logincontrol")]
        private void HandleLoginEnable(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                scene.LoginControl.Ready(SceneInterface.ReadyFlags.LoginsEnable);
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.logincontrol")]
        private void HandleLoginDisable(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                scene.LoginControl.NotReady(SceneInterface.ReadyFlags.LoginsEnable);
                m_WebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion

        #region Start/Stop Control
        [AdminWebIfRequiredRight("regions.control")]
        private void HandleRestart(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("seconds"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                scene.RequestRegionRestart(jsondata["seconds"].AsInt);
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.control")]
        private void HandleRestartAbort(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                scene.AbortRegionRestart();
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.control")]
        private void HandleStart(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else if (m_Scenes.ContainsKey(region.ID))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.AlreadyStarted);
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
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.FailedToStart);
                    return;
                }
                m_Scenes.Add(si);
                si.LoadScene();
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.control")]
        private void HandleStop(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_Scenes.Remove(scene);
                m_WebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion

        #region Notices
        [AdminWebIfRequiredRight("region.notice")]
        private void HandleNotice(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("message"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                foreach(IAgent agent in scene.RootAgents)
                {
                    agent.SendRegionNotice(scene.Owner, jsondata["message"].ToString(), scene.ID);
                }
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("region.agents.notice")]
        private void HandleAgentNotice(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            IAgent agent;
            if (!jsondata.ContainsKey("id") ||
                !jsondata.ContainsKey("agentid") ||
                !jsondata.ContainsKey("message"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene) ||
                !scene.RootAgents.TryGetValue(jsondata["agentid"].AsUUID, out agent))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                agent.SendRegionNotice(scene.Owner, jsondata["message"].ToString(), scene.ID);
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.notice")]
        private void HandleNotices(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("message"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                foreach (var scene in m_Scenes.Values)
                {
                    foreach (var agent in scene.RootAgents)
                    {
                        agent.SendRegionNotice(scene.Owner, jsondata["message"].ToString(), scene.ID);
                    }
                }
                m_WebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion

        #region Enable/Disable
        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleEnable(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                try
                {
                    m_RegionStorage.AddRegionFlags(region.ID, RegionFlags.RegionOnline);
                    m_WebIF.SuccessResponse(req, new Map());
                }
                catch
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                }
            }
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleDisable(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                try
                {
                    m_RegionStorage.RemoveRegionFlags(region.ID, RegionFlags.RegionOnline);
                    m_WebIF.SuccessResponse(req, new Map());
                }
                catch
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                }
            }
        }
        #endregion

        #region Sim Console Control
        private const string EstateOwnerIsSimConsoleUser = "estate_owner_is_simconsole_user";
        private const string EstateManagerIsSimConsoleUser = "estate_manager_is_simconsole_user";
        private const string RegionOwnerIsSimConsoleUser = "region_owner_is_simconsole_user";

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleEnableEstateOwnerSimConsole(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("value"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                switch (jsondata["value"].ToString())
                {
                    case "enable":
                        m_ServerParams[region.ID, EstateOwnerIsSimConsoleUser] = "true";
                        break;

                    case "disable":
                        m_ServerParams[region.ID, EstateOwnerIsSimConsoleUser] = "false";
                        break;

                    case "global":
                        m_ServerParams.Remove(region.ID, EstateOwnerIsSimConsoleUser);
                        break;

                    default:
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                }

                var m = new Map
                {
                    { "effective", m_ServerParams.GetBoolean(region.ID, EstateOwnerIsSimConsoleUser, false) },
                    { "global", m_ServerParams.GetBoolean(UUID.Zero, EstateOwnerIsSimConsoleUser, false) }
                };
                m_WebIF.SuccessResponse(req, m);
            }
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleEnableEstateManagerSimConsole(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("value"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                switch (jsondata["value"].ToString())
                {
                    case "enable":
                        m_ServerParams[region.ID, EstateManagerIsSimConsoleUser] = "true";
                        break;

                    case "disable":
                        m_ServerParams[region.ID, EstateManagerIsSimConsoleUser] = "false";
                        break;

                    case "global":
                        m_ServerParams.Remove(region.ID, EstateManagerIsSimConsoleUser);
                        break;

                    default:
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                }

                var m = new Map
                {
                    { "effective", m_ServerParams.GetBoolean(region.ID, EstateManagerIsSimConsoleUser, false) },
                    { "global", m_ServerParams.GetBoolean(UUID.Zero, EstateManagerIsSimConsoleUser, false) }
                };
                m_WebIF.SuccessResponse(req, m);
            }
        }

        [AdminWebIfRequiredRight("regions.manage")]
        private void HandleEnableRegionOwnerSimConsole(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("value"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                switch (jsondata["value"].ToString())
                {
                    case "enable":
                        m_ServerParams[region.ID, RegionOwnerIsSimConsoleUser] = "true";
                        break;

                    case "disable":
                        m_ServerParams[region.ID, RegionOwnerIsSimConsoleUser] = "false";
                        break;

                    case "global":
                        m_ServerParams.Remove(region.ID, RegionOwnerIsSimConsoleUser);
                        break;

                    default:
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                }

                var m = new Map
                {
                    { "effective", m_ServerParams.GetBoolean(region.ID, RegionOwnerIsSimConsoleUser, false) },
                    { "global", m_ServerParams.GetBoolean(UUID.Zero, RegionOwnerIsSimConsoleUser, false) }
                };
                m_WebIF.SuccessResponse(req, m);
            }
        }

        #endregion

        #region Environment Control
        [AdminWebIfRequiredRight("regions.environmentcontrol")]
        public void HandleEnvironmentSet(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("parameter") || !jsondata.ContainsKey("value"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
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
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                }

                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.environmentcontrol")]
        public void HandleEnvironmentGet(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("parameter"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                var res = new Map();
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
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                }
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("regions.environmentcontrol")]
        public void HandleEnvironmentResetToDefaults(HttpRequest req, Map jsondata)
        {
            SceneInterface scene;
            if (!jsondata.ContainsKey("id") || !jsondata.ContainsKey("which"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if (!m_Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
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
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                }

                m_WebIF.SuccessResponse(req, new Map());
            }
        }
        #endregion
    }
    #endregion

    #region Factory
    [PluginName("RegionAdmin")]
    public class RegionAdminFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new RegionAdmin(
                ownSection.GetString("RegionStorage", "RegionStorage"),
                ownSection.GetString("SimulationDataStorage", "SimulationDataStorage"),
                ownSection.GetString("EstateService", "EstateService"));
    }
    #endregion
}