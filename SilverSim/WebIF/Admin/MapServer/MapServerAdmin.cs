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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.WebIF.Admin.MapServer
{
    [Description("WebIF MapServer Admin Support")]
    [PluginName("MapServerAdmin")]
    public class MapServerAdmin : IPlugin
    {
        private readonly string m_GridServiceName;
        private readonly string m_RegionDefaultFlagsServiceName;
        private GridServiceInterface m_GridService;
        private RegionDefaultFlagsServiceInterface m_RegionDefaultFlagsService;
        private IAdminWebIF m_WebIF;
        private readonly UUID m_ScopeID;

        public MapServerAdmin(IConfig ownSection)
        {
            m_ScopeID = new UUID(ownSection.GetString("ScopeID", UUID.Zero.ToString()));
            m_GridServiceName = ownSection.GetString("GridService", "GridService");
            m_RegionDefaultFlagsServiceName = ownSection.GetString("RegionDefaultFlagsService", "RegionDefaultFlagsService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_WebIF = loader.GetAdminWebIF();
            m_RegionDefaultFlagsService = loader.GetService<RegionDefaultFlagsServiceInterface>(m_RegionDefaultFlagsServiceName);
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            m_WebIF.ModuleNames.Add("mapserver");
            m_WebIF.AutoGrantRights["mapserver.unregister"].Add("mapserver.view");
            m_WebIF.AutoGrantRights["mapserver.manage"].Add("mapserver.view");
            m_WebIF.JsonMethods.Add("mapserver.search", HandleMapServerSearch);
            m_WebIF.JsonMethods.Add("mapserver.getdefaultregions", HandleMapServerGetDefaultRegions);
            m_WebIF.JsonMethods.Add("mapserver.getdefaulthgregions", HandleMapServerGetDefaultHGRegions);
            m_WebIF.JsonMethods.Add("mapserver.getfallbackregions", HandleMapServerGetFallbackRegions);
            m_WebIF.JsonMethods.Add("mapserver.unregister", HandleMapServerUnregisterRegion);
            m_WebIF.JsonMethods.Add("mapserver.defaultregionflags.list", HandleMapServerGetDefaultRegionFlags);
            m_WebIF.JsonMethods.Add("mapserver.defaultregionflags.change", HandleMapServerChangeDefaultRegionFlags);
        }

        private void ReturnRegionsResult(HttpRequest req, List<RegionInfo> regions)
        {
            var resdata = new Map();
            foreach (var ri in regions)
            {
                var owner = m_WebIF.ResolveName(ri.Owner);
                var regiondata = new Map
                {
                    { "name", ri.Name },
                    { "location_x", ri.Location.GridX },
                    { "location_y", ri.Location.GridY },
                    { "size_x", ri.Size.GridX },
                    { "size_y", ri.Size.GridY },
                    { "owner", owner.ToString() },
                    { "flags", (int)ri.Flags }
                };
                resdata.Add(ri.ID.ToString(), regiondata);
            }

            m_WebIF.SuccessResponse(req, resdata);
        }

        [AdminWebIfRequiredRight("mapserver.unregister")]
        private void HandleMapServerUnregisterRegion(HttpRequest req, Map jsondata)
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

        [AdminWebIfRequiredRight("mapserver.manage")]
        private void HandleMapServerChangeDefaultRegionFlags(HttpRequest req, Map jsondata)
        {
            UUID id;
            if (!jsondata.TryGetValue("id", out id))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                var setFlags = RegionFlags.None;
                var removeFlags = RegionFlags.None;

                bool flag;
                if(jsondata.TryGetValue("fallback", out flag))
                {
                    if(flag)
                    {
                        setFlags |= RegionFlags.FallbackRegion;
                    }
                    else
                    {
                        removeFlags |= RegionFlags.FallbackRegion;
                    }
                }
                if (jsondata.TryGetValue("default", out flag))
                {
                    if (flag)
                    {
                        setFlags |= RegionFlags.DefaultRegion;
                    }
                    else
                    {
                        removeFlags |= RegionFlags.DefaultRegion;
                    }
                }
                if (jsondata.TryGetValue("defaultintergrid", out flag))
                {
                    if (flag)
                    {
                        setFlags |= RegionFlags.DefaultIntergridRegion;
                    }
                    else
                    {
                        removeFlags |= RegionFlags.DefaultIntergridRegion;
                    }
                }
                if (jsondata.TryGetValue("persistent", out flag))
                {
                    if (flag)
                    {
                        setFlags |= RegionFlags.Persistent;
                    }
                    else
                    {
                        removeFlags |= RegionFlags.Persistent;
                    }
                }

                try
                {
                    m_GridService.AddRegionFlags(id, setFlags);
                    m_GridService.RemoveRegionFlags(id, removeFlags);
                    m_RegionDefaultFlagsService.ChangeRegionDefaultFlags(id, setFlags, removeFlags);
                }
                catch
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                    return;
                }
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("mapserver.manage")]
        private void HandleMapServerGetDefaultRegionFlags(HttpRequest req, Map jsondata)
        {
            var resdata = new Map();
            foreach (var kvp in m_RegionDefaultFlagsService.GetAllRegionDefaultFlags())
            {
                resdata.Add(kvp.Key.ToString(), (int)kvp.Value);
            }
            m_WebIF.SuccessResponse(req, resdata);
        }

        [AdminWebIfRequiredRight("mapserver.view")]
        private void HandleMapServerGetDefaultHGRegions(HttpRequest req, Map jsondata)
        {
            List<RegionInfo> regions;
            try
            {
                regions = m_GridService.GetDefaultIntergridRegions(m_ScopeID);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            ReturnRegionsResult(req, regions);
        }

        [AdminWebIfRequiredRight("mapserver.view")]
        private void HandleMapServerGetDefaultRegions(HttpRequest req, Map jsondata)
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
        private void HandleMapServerGetFallbackRegions(HttpRequest req, Map jsondata)
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
        private void HandleMapServerSearch(HttpRequest req, Map jsondata)
        {
            string searchkey;
            if(!jsondata.TryGetValue("query", out searchkey))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            List<RegionInfo> regions;
            try
            {
                regions = m_GridService.SearchRegionsByName(UUID.Zero, searchkey);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            ReturnRegionsResult(req, regions);
        }
    }
}
