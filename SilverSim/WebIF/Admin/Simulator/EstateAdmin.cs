// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.WebIF.Admin.Simulator
{
    #region Service implementation
    [Description("WebIF Estate Admin Support")]
    public class EstateAdmin : IPlugin
    {
        readonly string m_EstateServiceName;
        readonly string m_RegionStorageName;
        EstateServiceInterface m_EstateService;
        GridServiceInterface m_RegionStorageService;
        IAdminWebIF m_WebIF;
        SceneList m_Scenes;

        public EstateAdmin(string estateServiceName, string regionStorageName)
        {
            m_EstateServiceName = estateServiceName;
            m_RegionStorageName = regionStorageName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            m_RegionStorageService = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            IAdminWebIF webif = loader.GetAdminWebIF();
            m_WebIF = webif;
            webif.JsonMethods.Add("estates.list", HandleList);
            webif.JsonMethods.Add("estate.get", HandleGet);
            webif.JsonMethods.Add("estate.update", HandleUpdate);
            webif.JsonMethods.Add("estate.delete", HandleDelete);
            webif.JsonMethods.Add("estate.create", HandleCreate);
            webif.JsonMethods.Add("estate.notice", HandleNotice);

            webif.AutoGrantRights["estates.manage"].Add("estates.view");
            webif.AutoGrantRights["estate.notice"].Add("estates.view");
        }

        [AdminWebIfRequiredRight("estates.view")]
        void HandleList(HttpRequest req, Map jsondata)
        {
            List<EstateInfo> estates = m_EstateService.All;

            Map res = new Map();
            AnArray estateRes = new AnArray();
            foreach (EstateInfo estate in estates)
            {
                estate.Owner = m_WebIF.ResolveName(estate.Owner);
                estateRes.Add(estate.ToJsonMap());
            }
            res.Add("estates", estateRes);
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("estates.view")]
        void HandleGet(HttpRequest req, Map jsondata)
        {
            EstateInfo estateInfo;
            if ((jsondata.ContainsKey("name") && 
                    m_EstateService.TryGetValue(jsondata["name"].ToString(), out estateInfo)) ||
                (jsondata.ContainsKey("id") && 
                    m_EstateService.TryGetValue(jsondata["id"].AsUInt, out estateInfo)))
            {
                /* found estate via name or via id */
            }
            else
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            Map res = new Map();
            estateInfo.Owner = m_WebIF.ResolveName(estateInfo.Owner);
            res.Add("estate", estateInfo.ToJsonMap());
            List<UUID> regionMap = m_EstateService.RegionMap[estateInfo.ID];
            AnArray regionsdata = new AnArray();
            foreach(UUID regionid in regionMap)
            {
                RegionInfo rInfo;
                Map regiondata = new Map();

                regiondata.Add("ID", regionid);
                if (m_RegionStorageService.TryGetValue(regionid, out rInfo))
                {
                    regiondata.Add("Name", rInfo.Name);
                }
                regionsdata.Add(regiondata);
            }
            res.Add("regions", regionsdata);

            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("estates.manage")]
        void HandleUpdate(HttpRequest req, Map jsondata)
        {
            EstateInfo estateInfo;
            if (jsondata.ContainsKey("id") && m_EstateService.TryGetValue(jsondata["id"].AsUInt, out estateInfo))
            {
                /* found estate via id */
            }
            else
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            if (jsondata.ContainsKey("owner") &&
                !m_WebIF.TranslateToUUI(jsondata["owner"].ToString(), out estateInfo.Owner))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                return;
            }

            try
            {
                if (jsondata.ContainsKey("name"))
                {
                    estateInfo.Name = jsondata["name"].ToString();
                }

                if (jsondata.ContainsKey("flags"))
                {
                    estateInfo.Flags = (RegionOptionFlags)jsondata["flags"].AsUInt;
                }

                if (jsondata.ContainsKey("pricepermeter"))
                {
                    estateInfo.PricePerMeter = jsondata["pricepermeter"].AsInt;
                }

                if (jsondata.ContainsKey("billablefactor"))
                {
                    estateInfo.BillableFactor = jsondata["billablefactor"].AsReal;
                }

                if (jsondata.ContainsKey("abuseemail"))
                {
                    estateInfo.AbuseEmail = jsondata["abuseemail"].ToString();
                }

                if (jsondata.ContainsKey("parentestateid"))
                {
                    estateInfo.ParentEstateID = jsondata["parentestateid"].AsUInt;
                }

            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            try
            {
                m_EstateService[estateInfo.ID] = estateInfo;
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }

            List<UUID> regionids = m_EstateService.RegionMap[estateInfo.ID];
            foreach(UUID regionid in regionids)
            {
                SceneInterface scene;
                if(m_Scenes.TryGetValue(regionid, out scene))
                {
                    scene.TriggerEstateUpdate();
                }
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("estates.manage")]
        void HandleCreate(HttpRequest req, Map jsondata)
        {
            EstateInfo estateInfo = new EstateInfo();
            if (!m_WebIF.TranslateToUUI(jsondata["owner"].ToString(), out estateInfo.Owner))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                return;
            }
            try
            {
                if (jsondata.ContainsKey("id"))
                {
                    estateInfo.ID = jsondata["id"].AsUInt;
                }
                else
                {
                    List<uint> estateids = m_EstateService.AllIDs;
                    uint id = 100;
                    while(estateids.Contains(id))
                    {
                        ++id;
                    }
                    estateInfo.ID = id;
                }
                estateInfo.Name = jsondata["name"].ToString();
                estateInfo.Flags = jsondata.ContainsKey("flags") ?
                    (RegionOptionFlags)jsondata["flags"].AsUInt :
                    RegionOptionFlags.AllowVoice | RegionOptionFlags.AllowSetHome | RegionOptionFlags.AllowLandmark | RegionOptionFlags.AllowDirectTeleport | RegionOptionFlags.AllowParcelChanges | RegionOptionFlags.ExternallyVisible;

                estateInfo.PricePerMeter = jsondata["pricepermeter"].AsInt;
                estateInfo.BillableFactor = jsondata["billablefactor"].AsReal;
                estateInfo.AbuseEmail = jsondata["abuseemail"].ToString();
                estateInfo.ParentEstateID = jsondata["parentestateid"].AsUInt;
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            try
            {
                m_EstateService.Add(estateInfo);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("estates.manage")]
        void HandleDelete(HttpRequest req, Map jsondata)
        {
            uint estateID;
            try
            {
                estateID = jsondata["id"].AsUInt;
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            if(m_EstateService.RegionMap[estateID].Count != 0)
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InUse);
                return;
            }

            try
            {
                m_EstateService[estateID] = null;
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("estate.notice")]
        void HandleNotice(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("id") || !jsondata.ContainsKey("message"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                uint estateID = jsondata["id"].AsUInt;
                List<UUID> regionIds = m_EstateService.RegionMap[estateID];

                if(regionIds.Count == 0)
                {
                    if (m_EstateService.ContainsKey(estateID))
                    {
                        Map m = new Map();
                        m.Add("noticed_regions", new AnArray());
                        m_WebIF.SuccessResponse(req, m);
                    }
                    else
                    {
                        m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                    }
                }
                else
                {
                    string message = jsondata["message"].ToString();
                    AnArray regions = new AnArray();

                    foreach(UUID regionId in regionIds)
                    {
                        SceneInterface si;
                        if(m_Scenes.TryGetValue(regionId, out si))
                        {
                            regions.Add(regionId);
                            UUI regionOwner = si.Owner;
                            foreach(IAgent agent in si.RootAgents)
                            {
                                agent.SendRegionNotice(regionOwner, message, regionId);
                            }
                        }
                    }
                    Map m = new Map();
                    m.Add("noticed_regions", regions);
                    m_WebIF.SuccessResponse(req, m);
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("EstateAdmin")]
    public class EstateAdminFactory : IPluginFactory
    {
        public EstateAdminFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new EstateAdmin(
                ownSection.GetString("EstateService", "EstateService"),
                ownSection.GetString("RegionStorage", "RegionStorage"));
        }
    }
    #endregion
}
