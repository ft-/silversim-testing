// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.WebIF.Admin.MapServer
{
    public class MapServerAdmin : IPlugin
    {
        readonly string m_GridServiceName;
        readonly string m_AvatarNameServicesNames;
        readonly string m_RegionDefaultFlagsServiceName;
        readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();
        GridServiceInterface m_GridService;
        RegionDefaultFlagsServiceInterface m_RegionDefaultFlagsService;
        IAdminWebIF m_WebIF;
        UUID m_ScopeID;

        public MapServerAdmin(IConfig ownSection)
        {
            m_ScopeID = new UUID(ownSection.GetString("ScopeID", UUID.Zero.ToString()));
            m_GridServiceName = ownSection.GetString("GridService", "GridService");
            m_RegionDefaultFlagsServiceName = ownSection.GetString("RegionDefaultFlagsService", "RegionDefaultFlagsService");
            m_AvatarNameServicesNames = ownSection.GetString("AvatarNameServices", "AvatarNameStorage").Trim();
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_WebIF = loader.GetAdminWebIF();
            m_RegionDefaultFlagsService = loader.GetService<RegionDefaultFlagsServiceInterface>(m_RegionDefaultFlagsServiceName);
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            if (0 != m_AvatarNameServicesNames.Length)
            {
                string[] names = m_AvatarNameServicesNames.Split(',');
                foreach(string name in names)
                {
                    m_AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(name.Trim()));
                }
            }

            m_WebIF.AutoGrantRights["mapserver.manage"].Add("mapserver.view");
            m_WebIF.JsonMethods.Add("mapserver.search", HandleMapServerSearch);
            m_WebIF.JsonMethods.Add("mapserver.getdefaultregions", HandleMapServerGetDefaultRegions);
            m_WebIF.JsonMethods.Add("mapserver.getdefaulthgregions", HandleMapServerGetDefaultHGRegions);
            m_WebIF.JsonMethods.Add("mapserver.getfallbackregions", HandleMapServerGetFallbackRegions);
            m_WebIF.JsonMethods.Add("mapserver.unregister", HandleMapServerUnregisterRegion);
        }

        public UUI ResolveUUI(UUI id)
        {
            UUI foundid;
            foreach(AvatarNameServiceInterface service in m_AvatarNameServices)
            {
                if(service.TryGetValue(id, out foundid))
                {
                    id = foundid;
                    if(id.IsAuthoritative)
                    {
                        break;
                    }
                }
            }
            return id;
        }

        void ReturnRegionsResult(HttpRequest req, List<RegionInfo> regions)
        {
            Map resdata = new Map();
            foreach (RegionInfo ri in regions)
            {
                Map regiondata = new Map();
                regiondata.Add("name", ri.Name);
                regiondata.Add("location_x", ri.Location.GridX);
                regiondata.Add("location_y", ri.Location.GridY);
                regiondata.Add("size_x", ri.Size.GridX);
                regiondata.Add("size_y", ri.Size.GridY);
                UUI owner = ResolveUUI(ri.Owner);
                regiondata.Add("owner", owner.ToString());
                regiondata.Add("flags", (int)ri.Flags);
                resdata.Add(ri.ID.ToString(), regiondata);
            }

            m_WebIF.SuccessResponse(req, resdata);
        }

        [AdminWebIfRequiredRight("mapserver.unregister")]
        void HandleMapServerUnregisterRegion(HttpRequest req, Map jsondata)
        {
            UUID regionId;
            if(!jsondata.TryGetValue("id", out regionId))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else if(!m_GridService.ContainsKey(m_ScopeID, regionId))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                m_GridService.UnregisterRegion(m_ScopeID, regionId);
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("mapserver.view")]
        void HandleMapServerGetDefaultHGRegions(HttpRequest req, Map jsondata)
        {
            List<RegionInfo> regions;
            try
            {
                regions = m_GridService.GetDefaultHypergridRegions(m_ScopeID);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            ReturnRegionsResult(req, regions);
        }

        [AdminWebIfRequiredRight("mapserver.view")]
        void HandleMapServerGetDefaultRegions(HttpRequest req, Map jsondata)
        {
            List<RegionInfo> regions;
            try
            {
                regions = m_GridService.GetDefaultRegions(m_ScopeID);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            ReturnRegionsResult(req, regions);
        }

        [AdminWebIfRequiredRight("mapserver.view")]
        void HandleMapServerGetFallbackRegions(HttpRequest req, Map jsondata)
        {
            List<RegionInfo> regions;
            try
            {
                regions = m_GridService.GetFallbackRegions(m_ScopeID);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            ReturnRegionsResult(req, regions);
        }

        [AdminWebIfRequiredRight("mapserver.view")]
        void HandleMapServerSearch(HttpRequest req, Map jsondata)
        {
            IValue searchkey;
            if(!jsondata.TryGetValue("query", out searchkey))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            List<RegionInfo> regions;
            try
            {
                regions = m_GridService.SearchRegionsByName(UUID.Zero, searchkey.ToString());
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            ReturnRegionsResult(req, regions);
        }
    }

    [PluginName("MapServerAdmin")]
    public class MapServerAdminFactory : IPluginFactory
    {
        public MapServerAdminFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MapServerAdmin(ownSection);
        }
    }
}
