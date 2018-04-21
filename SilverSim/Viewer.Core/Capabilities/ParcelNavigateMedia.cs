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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Messages.Parcel;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ParcelNavigateMedia : ICapabilityInterface
    {
        private readonly UGUI m_Agent;
        private readonly SceneInterface m_Scene;
        private readonly string m_RemoteIP;

        public ParcelNavigateMedia(UGUI agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName => "ParcelNavigateMedia";

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
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            if(reqmap["agent-id"].AsUUID != m_Agent.ID)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            var localID = reqmap["local-id"].AsInt; /* this is parcel local id */
            var url = reqmap["url"].ToString();

            ParcelInfo parcelInfo;
            if(m_Scene.Parcels.TryGetValue(localID, out parcelInfo))
            {
                if (!m_Scene.CanEditParcelDetails(m_Agent, parcelInfo))
                {
                    return;
                }
                parcelInfo.MediaURI = new URI(url);
                var pmu = new ParcelMediaUpdate
                {
                    MediaAutoScale = parcelInfo.MediaAutoScale,
                    MediaDesc = parcelInfo.MediaDescription,
                    MediaHeight = parcelInfo.MediaHeight,
                    MediaID = parcelInfo.MediaID,
                    MediaLoop = parcelInfo.MediaLoop,
                    MediaType = parcelInfo.MediaType,
                    MediaURL = url,
                    MediaWidth = parcelInfo.MediaWidth
                };
                parcelInfo.MediaAutoScale = pmu.MediaAutoScale;
                parcelInfo.MediaDescription = pmu.MediaDesc;
                parcelInfo.MediaHeight = pmu.MediaHeight;
                parcelInfo.MediaID = pmu.MediaID;
                parcelInfo.MediaType = pmu.MediaType;
                parcelInfo.MediaLoop = pmu.MediaLoop;
                parcelInfo.MediaURI = new URI(pmu.MediaURL);
                parcelInfo.MediaWidth = pmu.MediaWidth;
                m_Scene.Parcels.Store(parcelInfo.ID);

                foreach (var rootAgent in m_Scene.RootAgents)
                {
                    rootAgent.SendMessageIfRootAgent(pmu, m_Scene.ID);
                }
            }

            var m = new Map();
            using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                resp.ContentType = "application/llsd+xml";
                using (var s = resp.GetOutputStream())
                {
                    LlsdXml.Serialize(m, s);
                }
            }
        }
    }
}
