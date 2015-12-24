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
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.WebIF.Admin.Simulator
{
    #region Service implementation
    public class RegionAdmin : IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ADMIN WEB IF - REGION");

        readonly string m_RegionStorageName;
        readonly string m_SimulationDataName;
        GridServiceInterface m_RegionStorage;
        SceneFactoryInterface m_SceneFactory;
        SimulationDataStorageInterface m_SimulationData;

        public RegionAdmin(string regionStorageName, string simulationDataName)
        {
            m_RegionStorageName = regionStorageName;
            m_SimulationDataName = simulationDataName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_SimulationData = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataName);

            AdminWebIF webif = loader.GetAdminWebIF();
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
                ownSection.GetString("SimulationDataStorage", "SimulationDataStorage"));
        }
    }
    #endregion
}