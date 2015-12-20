// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
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
            List<AdminWebIF> webIF = loader.GetServicesByValue<AdminWebIF>();
            if (webIF.Count == 0)
            {
                throw new ConfigurationLoader.ConfigurationErrorException("No Admin WebIF service configured");
            }
            webIF[0].JsonMethods.Add("estates.list", HandleList);
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
        void HandleGet(HttpRequest req, Map jsondata, List<string> rights)
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

            EstateInfo estateInfo = new EstateInfo();
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
