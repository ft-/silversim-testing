// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class AgentPreferences : ICapabilityInterface
    {
        readonly ViewerAgent m_Agent;
        readonly IPEndPoint m_RemoteEndPoint;

        public AgentPreferences(ViewerAgent agent, IPEndPoint ep)
        {
            m_Agent = agent;
            m_RemoteEndPoint = ep;
        }

        public string CapabilityName
        {
            get
            {
                return "AgentPreferences";
            }
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if(!httpreq.CallerIP.Equals(m_RemoteEndPoint))
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

            if(reqmap.ContainsKey("hover_height"))
            {
                /* store hover height */
                m_Agent.HoverHeight = reqmap["hover_height"].AsReal;
            }

            if(reqmap.ContainsKey("default_object_perm_masks"))
            {
                Map defobjectperms = reqmap["default_object_perm_masks"] as Map;
                if(defobjectperms != null)
                {
                    /* Group, Everyone, NextOwner fields need to be stored */
                }
            }

            using (HttpResponse httpres = httpreq.BeginResponse())
            {
                httpres.ContentType = "application/llsd+xml";
                using (Stream outStream = httpres.GetOutputStream())
                {
                    LlsdXml.Serialize(new Map(), outStream);
                }
            }
        }
    }
}
