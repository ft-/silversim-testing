// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Messages.Parcel;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ParcelNavigateMedia : ICapabilityInterface
    {
        readonly UUI m_Agent;
        readonly SceneInterface m_Scene;
        readonly string m_RemoteIP;

        public ParcelNavigateMedia(UUI agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName
        {
            get
            {
                return "ParcelNavigateMedia";
            }
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != m_RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(httpreq.Body) as Map;
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (null == reqmap)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            if(reqmap["agent-id"].AsUUID != m_Agent.ID)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            int localID = reqmap["local-id"].AsInt; /* this is parcel local id */
            string url = reqmap["url"].ToString();

            ParcelInfo parcelInfo;
            if(m_Scene.Parcels.TryGetValue(localID, out parcelInfo))
            {
                if (!m_Scene.CanEditParcelDetails(m_Agent, parcelInfo))
                {
                    return;
                }
                parcelInfo.MediaURI = new URI(url);
                ParcelMediaUpdate pmu = new ParcelMediaUpdate();
                pmu.MediaAutoScale = parcelInfo.MediaAutoScale;
                pmu.MediaDesc = parcelInfo.MediaDescription;
                pmu.MediaHeight = parcelInfo.MediaHeight;
                pmu.MediaID = parcelInfo.MediaID;
                pmu.MediaLoop = parcelInfo.MediaLoop;
                pmu.MediaType = parcelInfo.MediaType;
                pmu.MediaURL = url;
                pmu.MediaWidth = parcelInfo.MediaWidth;

                parcelInfo.MediaAutoScale = pmu.MediaAutoScale;
                parcelInfo.MediaDescription = pmu.MediaDesc;
                parcelInfo.MediaHeight = pmu.MediaHeight;
                parcelInfo.MediaID = pmu.MediaID;
                parcelInfo.MediaType = pmu.MediaType;
                parcelInfo.MediaLoop = pmu.MediaLoop;
                parcelInfo.MediaURI = new URI(pmu.MediaURL);
                parcelInfo.MediaWidth = pmu.MediaWidth;
                m_Scene.Parcels.Store(parcelInfo.ID);

                foreach (IAgent rootAgent in m_Scene.RootAgents)
                {
                    rootAgent.SendMessageIfRootAgent(pmu, m_Scene.ID);
                }
            }

            Map m = new Map();
            using (HttpResponse resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                resp.ContentType = "application/llsd+xml";
                using (Stream s = resp.GetOutputStream())
                {
                    LlsdXml.Serialize(m, s);
                }
            }
        }
    }
}
