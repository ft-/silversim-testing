// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using SilverSim.Types.Estate;
using System.Collections.Generic;

namespace SilverSim.WebIF.Admin.Simulator
{
    #region Service implementation
    public class EstateAdmin : IPlugin
    {
        string m_EstateServiceName;
        EstateServiceInterface m_EstateService;

        public EstateAdmin(string estateServiceName)
        {
            m_EstateServiceName = estateServiceName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            AdminWebIF webif = loader.GetAdminWebIF();
            webif.JsonMethods.Add("estates.list", HandleList);
            webif.JsonMethods.Add("estate.get", HandleGet);
            webif.JsonMethods.Add("estate.update", HandleUpdate);
            webif.JsonMethods.Add("estate.delete", HandleDelete);
            webif.JsonMethods.Add("estate.create", HandleCreate);
            webif.JsonMethods.Add("estate.notice", HandleNotice);
        }

        [AdminWebIF.RequiredRight("estates.view")]
        void HandleList(HttpRequest req, Map jsondata)
        {
            List<EstateInfo> estates = m_EstateService.All;

            Map res = new Map();
            AnArray estateRes = new AnArray();
            foreach (EstateInfo estate in estates)
            {
                estateRes.Add(estate.ToJsonMap());
            }
            res.Add("estates", estateRes);
            AdminWebIF.SuccessResponse(req, res);
        }

        [AdminWebIF.RequiredRight("estates.view")]
        void HandleGet(HttpRequest req, Map jsondata)
        {
            EstateInfo estateInfo;
            if (jsondata.ContainsKey("name") && m_EstateService.TryGetValue(jsondata["name"].ToString(), out estateInfo))
            {
                /* found estate via name */
            }
            else if (jsondata.ContainsKey("id") && m_EstateService.TryGetValue(jsondata["id"].AsUInt, out estateInfo))
            {
                /* found estate via id */
            }
            else
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
                return;
            }

            Map res = new Map();
            res.Add("estate", estateInfo.ToJsonMap());
            AdminWebIF.SuccessResponse(req, res);
        }

        [AdminWebIF.RequiredRight("estates.manage")]
        void HandleUpdate(HttpRequest req, Map jsondata)
        {
            EstateInfo estateInfo;
            if (jsondata.ContainsKey("id") && m_EstateService.TryGetValue(jsondata["id"].AsUInt, out estateInfo))
            {
                /* found estate via id */
            }
            else
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
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

                if (jsondata.ContainsKey("owner"))
                {
                    estateInfo.Owner = new UUI(jsondata["owner"].ToString());
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
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }

            try
            {
                m_EstateService[estateInfo.ID] = estateInfo;
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
            }

        }

        [AdminWebIF.RequiredRight("estates.manage")]
        void HandleCreate(HttpRequest req, Map jsondata)
        {
            EstateInfo estateInfo = new EstateInfo();
            try
            {
                estateInfo.ID = jsondata["id"].AsUInt;
                estateInfo.Name = jsondata["name"].ToString();
                estateInfo.Flags = (RegionOptionFlags)jsondata["flags"].AsUInt;
                estateInfo.Owner = new UUI(jsondata["owner"].ToString());
                estateInfo.PricePerMeter = jsondata["pricepermeter"].AsInt;
                estateInfo.BillableFactor = jsondata["billablefactor"].AsReal;
                estateInfo.AbuseEmail = jsondata["abuseemail"].ToString();
                estateInfo.ParentEstateID = jsondata["parentestateid"].AsUInt;
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }

            try
            {
                m_EstateService.Add(estateInfo);
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
            }
        }

        [AdminWebIF.RequiredRight("estates.manage")]
        void HandleDelete(HttpRequest req, Map jsondata)
        {
            uint estateID;
            try
            {
                estateID = jsondata["id"].AsUInt;
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
                return;
            }

            if(m_EstateService.RegionMap[estateID].Count != 0)
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InUse);
                return;
            }

            try
            {
                m_EstateService[estateID] = null;
            }
            catch
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotPossible);
            }
        }

        [AdminWebIF.RequiredRight("estate.notice")]
        void HandleNotice(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("id") || !jsondata.ContainsKey("message"))
            {
                AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.InvalidRequest);
            }
            else
            {
                List<UUID> regionIds = m_EstateService.RegionMap[jsondata["id"].AsUInt];

                if(regionIds.Count == 0)
                {
                    AdminWebIF.ErrorResponse(req, AdminWebIF.ErrorResult.NotFound);
                }
                else
                {
                    string message = jsondata["message"].ToString();

                    foreach(UUID regionId in regionIds)
                    {
                        SceneInterface si;
                        if(SceneManager.Scenes.TryGetValue(regionId, out si))
                        {
                            foreach(IAgent agent in si.RootAgents)
                            {
                                agent.SendRegionNotice(si.RegionData.Owner, message, regionId);
                            }
                        }
                    }
                    AdminWebIF.SuccessResponse(req, new Map());
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
            return new EstateAdmin(ownSection.GetString("EstateService", "EstateService"));
        }
    }
    #endregion
}
