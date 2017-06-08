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
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using System.ComponentModel;
using System.IO;

namespace SilverSim.WebIF.Admin.Simulator
{
    #region Service implementation
    [Description("WebIF Estate Admin Support")]
    [PluginName("EstateAdmin")]
    public class EstateAdmin : IPlugin
    {
        private readonly string m_EstateServiceName;
        private readonly string m_RegionStorageName;
        private EstateServiceInterface m_EstateService;
        private GridServiceInterface m_RegionStorageService;
        private IAdminWebIF m_WebIF;
        private SceneList m_Scenes;

        public EstateAdmin(IConfig ownSection)
        {
            m_EstateServiceName = ownSection.GetString("EstateService", "EstateService");
            m_RegionStorageName = ownSection.GetString("RegionStorage", "RegionStorage");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            m_RegionStorageService = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            IAdminWebIF webif = loader.GetAdminWebIF();
            m_WebIF = webif;
            webif.ModuleNames.Add("estates");
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
        private void HandleList(HttpRequest req, Map jsondata)
        {
            var estates = m_EstateService.All;

            var res = new Map();
            var estateRes = new AnArray();
            foreach (EstateInfo estate in estates)
            {
                estate.Owner = m_WebIF.ResolveName(estate.Owner);
                estateRes.Add(estate.ToJsonMap());
            }
            res.Add("estates", estateRes);
            m_WebIF.SuccessResponse(req, res);
        }

        [AdminWebIfRequiredRight("estates.view")]
        private void HandleGet(HttpRequest req, Map jsondata)
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

            var res = new Map();
            estateInfo.Owner = m_WebIF.ResolveName(estateInfo.Owner);
            res.Add("estate", estateInfo.ToJsonMap());
            var regionMap = m_EstateService.RegionMap[estateInfo.ID];
            var regionsdata = new AnArray();
            foreach(UUID regionid in regionMap)
            {
                RegionInfo rInfo;
                var regiondata = new Map
                {
                    ["ID"] = regionid
                };
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
        private void HandleUpdate(HttpRequest req, Map jsondata)
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
                m_EstateService.Update(estateInfo);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }

            foreach(UUID regionid in m_EstateService.RegionMap[estateInfo.ID])
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
        private void HandleCreate(HttpRequest req, Map jsondata)
        {
            var estateInfo = new EstateInfo();
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
                    var estateids = m_EstateService.AllIDs;
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
        private void HandleDelete(HttpRequest req, Map jsondata)
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
                if(!m_EstateService.Remove(estateID))
                {
                    throw new InvalidDataException();
                }
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("estate.notice")]
        private void HandleNotice(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("id") || !jsondata.ContainsKey("message"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                uint estateID = jsondata["id"].AsUInt;
                var regionIds = m_EstateService.RegionMap[estateID];

                if(regionIds.Count == 0)
                {
                    if (m_EstateService.ContainsKey(estateID))
                    {
                        var m = new Map
                        {
                            ["noticed_regions"] = new AnArray()
                        };
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
                    var regions = new AnArray();

                    foreach(var regionId in regionIds)
                    {
                        SceneInterface si;
                        if(m_Scenes.TryGetValue(regionId, out si))
                        {
                            regions.Add(regionId);
                            var regionOwner = si.Owner;
                            foreach(var agent in si.RootAgents)
                            {
                                agent.SendRegionNotice(regionOwner, message, regionId);
                            }
                        }
                    }
                    var m = new Map
                    {
                        ["noticed_regions"] = regions
                    };
                    m_WebIF.SuccessResponse(req, m);
                }
            }
        }
    }
    #endregion
}
