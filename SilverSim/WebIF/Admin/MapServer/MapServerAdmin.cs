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
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.WebIF.Admin.MapServer
{
    [Description("WebIF MapServer Admin Support")]
    public class MapServerAdmin : IPlugin
    {
        readonly string m_GridServiceName;
        readonly string m_RegionDefaultFlagsServiceName;
        GridServiceInterface m_GridService;
        RegionDefaultFlagsServiceInterface m_RegionDefaultFlagsService;
        IAdminWebIF m_WebIF;
        readonly UUID m_ScopeID;

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
                UUI owner = m_WebIF.ResolveName(ri.Owner);
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

        [AdminWebIfRequiredRight("mapserver.manage")]
        void HandleMapServerChangeDefaultRegionFlags(HttpRequest req, Map jsondata)
        {
            UUID id;
            if (!jsondata.TryGetValue("id", out id))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                RegionFlags setFlags = RegionFlags.None;
                RegionFlags removeFlags = RegionFlags.None;

                IValue iv;
                if(jsondata.TryGetValue("fallback", out iv))
                {
                    if(iv.AsBoolean)
                    {
                        setFlags |= RegionFlags.FallbackRegion;
                    }
                    else
                    {
                        removeFlags |= RegionFlags.FallbackRegion;
                    }
                }
                if (jsondata.TryGetValue("default", out iv))
                {
                    if (iv.AsBoolean)
                    {
                        setFlags |= RegionFlags.DefaultRegion;
                    }
                    else
                    {
                        removeFlags |= RegionFlags.DefaultRegion;
                    }
                }
                if (jsondata.TryGetValue("defaulthg", out iv))
                {
                    if (iv.AsBoolean)
                    {
                        setFlags |= RegionFlags.DefaultHGRegion;
                    }
                    else
                    {
                        removeFlags |= RegionFlags.DefaultHGRegion;
                    }
                }
                if (jsondata.TryGetValue("persistent", out iv))
                {
                    if (iv.AsBoolean)
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
        void HandleMapServerGetDefaultRegionFlags(HttpRequest req, Map jsondata)
        {
            Map resdata = new Map();
            foreach (KeyValuePair<UUID, RegionFlags> kvp in m_RegionDefaultFlagsService.GetAllRegionDefaultFlags())
            {
                resdata.Add(kvp.Key.ToString(), (int)kvp.Value);
            }
            m_WebIF.SuccessResponse(req, resdata);
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
