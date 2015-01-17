/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SilverSim.BackendHandlers.OpenSim.Simulation.Neighbor
{
    public class OpenSimNeighborHandler : IPlugin
    {
        protected static readonly ILog m_Log = LogManager.GetLogger("OPENSIM NEIGHBOR HANDLER");
        private BaseHttpServer m_HttpServer;
        public List<NeighborServiceInterface> m_NeighborServices = new List<NeighborServiceInterface>();

        public OpenSimNeighborHandler()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing handler for /region");
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/region", RegionPostHandler);

            List<NeighborServiceInterface> neighborservices = loader.GetServicesByValue<NeighborServiceInterface>();
            foreach(NeighborServiceInterface service in neighborservices)
            {
                if(service.ServiceType == NeighborServiceInterface.ServiceTypeEnum.Local)
                {
                    m_NeighborServices.Add(service);
                }
            }
        }

        private void GetRegionParams(string uri, out UUID regionID)
        {
            /* /region/<UUID> */
            regionID = UUID.Zero;

            uri = uri.Trim(new char[] { '/' });
            string[] parts = uri.Split('/');
            if(parts.Length < 2)
            {
                throw new InvalidDataException();
            }
            else
            {
                regionID = UUID.Parse(parts[1]);
            }
        }

        public void RegionPostHandler(HttpRequest req)
        {
            UUID regionID;
            try
            {
                GetRegionParams(req.RawUrl, out regionID);
            }
            catch
            {
                throw new InvalidDataException();
            }

            if (req.Method != "POST")
            {
                req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            if (req.ContentType != "application/json")
            {
                req.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported media type").Close();
                return;
            }

            IValue v;
            try
            {
                v = JSON.Deserialize(req.Body);
            }
            catch
            {
                req.BeginResponse(HttpStatusCode.BadRequest, "Bad Request").Close();
                return;
            }

            if (!(v is Map))
            {
                req.BeginResponse(HttpStatusCode.BadRequest, "Bad Request").Close();
                return;
            }

            Map m = (Map)v;
            if (!m.ContainsKey("destination_handle"))
            {
                req.BeginResponse(HttpStatusCode.BadRequest, "Bad Request").Close();
                return;
            }

            RegionInfo fromRegion = new RegionInfo();
            HttpResponse resp;

            try
            {
                fromRegion.ID = m["region_id"].AsUUID;
                if (m.ContainsKey("region_name"))
                {
                    fromRegion.Name = m["region_name"].ToString();
                }
                fromRegion.ServerHttpPort = m["http_port"].AsUInt;
                fromRegion.ServerURI = m["server_uri"].ToString();
                fromRegion.Location.X = m["region_xloc"].AsUInt;
                fromRegion.Location.Y = m["region_yloc"].AsUInt;
                fromRegion.Size.X = m["region_size_x"].AsUInt;
                fromRegion.Size.Y = m["region_size_y"].AsUInt;
#warning check whether to use external_host_name here instead of internal_ep_address
                fromRegion.ServerIP = m["internal_ep_address"].ToString();
                fromRegion.ServerPort = m["internal_ep_port"].AsUInt;
                fromRegion.Flags = RegionFlags.RegionOnline;
                fromRegion.ProtocolVariant = RegionInfo.ProtocolVariantId.OpenSim;
            }
            catch
            {
                req.BeginResponse(HttpStatusCode.BadRequest, "Bad Request").Close();
                return;
            }

            SceneInterface scene;

            try
            {
                scene = SceneManager.Scenes[new GridVector(m["destination_handle"].AsULong)];
            }
            catch
            {
                m = new Map();
                m.Add("success", false);
                resp = req.BeginResponse();
                resp.ContentType = "application/json";
                JSON.Serialize(m, resp.GetOutputStream());
                resp.Close();
                return;
            }

            RegionInfo toRegion = scene.RegionData;

            foreach(NeighborServiceInterface service in m_NeighborServices)
            {
                try
                {
                    service.notifyNeighborStatus(fromRegion, toRegion);
                }
                catch
                {
                    m_Log.WarnFormat("Failed to notify local neighbor (from {0} (ID {1}) to {2} (ID {3})",
                        fromRegion.Name, fromRegion.ID,
                        toRegion.Name, toRegion.ID);
                }
            }

            m = new Map();
            m.Add("success", false);
            resp = req.BeginResponse();
            resp.ContentType = "application/json";
            JSON.Serialize(m, resp.GetOutputStream());
            resp.Close();
        }

        #region Service Factory
        [PluginName("OpenSimNeighborHandler")]
        public class OpenSimNeighborHandlerFactory : IPluginFactory
        {
            public OpenSimNeighborHandlerFactory()
            {

            }

            public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
            {
                return new OpenSimNeighborHandler();
            }
        }
        #endregion
    }
}
