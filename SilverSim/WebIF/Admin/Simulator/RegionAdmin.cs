// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Scene;
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

        string m_RegionStorageName;
        GridServiceInterface m_RegionStorage;
        SceneFactoryInterface m_SceneFactory;

        public RegionAdmin(string regionStorageName)
        {
            m_RegionStorageName = regionStorageName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");

            AdminWebIF webif = loader.GetAdminWebIF();
            webif.JsonMethods.Add("regions.list", HandleList);
            webif.JsonMethods.Add("region.start", HandleStart);
            webif.JsonMethods.Add("region.stop", HandleStop);
            webif.JsonMethods.Add("region.enable", HandleEnable);
            webif.JsonMethods.Add("region.disable", HandleDisable);
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

        [AdminWebIF.RequiredRight("regions.control")]
        void HandleStart(HttpRequest req, Map jsondata)
        {
            RegionInfo region;
            if (!jsondata.ContainsKey("id"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            if (SceneManager.Scenes.ContainsKey(region.ID))
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

            if (!SceneManager.Scenes.TryGetValue(jsondata["id"].AsUUID, out scene))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
            }
            else
            {
                SceneManager.Scenes.Remove(scene);
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

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
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

            if (!m_RegionStorage.TryGetValue(jsondata["id"].AsUUID, out region))
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
            return new RegionAdmin(ownSection.GetString("RegionStorage", "RegionStorage"));
        }
    }
    #endregion
}