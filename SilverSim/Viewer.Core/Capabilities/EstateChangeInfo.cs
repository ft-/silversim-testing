// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class EstateChangeInfo : ICapabilityInterface
    {
        readonly SceneInterface m_Scene;
        readonly ViewerAgent m_Agent;

        public string CapabilityName
        {
            get
            {
                return "EstateChangeInfo";
            }
        }

        public EstateChangeInfo(ViewerAgent agent, SceneInterface scene)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
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

            string estateName = reqmap["estate_name"].ToString();
            double sun_hour = reqmap["sun_hour"].AsReal;
            bool isSunFixed = reqmap["is_sun_fixed"].AsBoolean;
            bool isExternallyVisible = reqmap["is_externally_visible"].AsBoolean;
            bool allowDirectTeleport = reqmap["allow_direct_teleport"].AsBoolean;
            bool denyAnonymous = reqmap["deny_anonymous"].AsBoolean;
            bool denyAgeUnverified = reqmap["deny_age_unverified"].AsBoolean;
            bool allowVoiceChat = reqmap["allow_voice_chat"].AsBoolean;
            UUID invoiceID = reqmap["invoice"].AsUUID;

#warning Implement linkage

            using (HttpResponse res = httpreq.BeginResponse("text/plain"))
            {
                /* no further action required */
            }
        }
    }
}
