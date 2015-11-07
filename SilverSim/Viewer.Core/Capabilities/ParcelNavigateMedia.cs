// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using ThreadedClasses;
using SilverSim.Scene.Types.Scene;
using System.IO;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ParcelNavigateMedia : ICapabilityInterface
    {
        readonly UUI m_Agent;
        readonly SceneInterface m_Scene;

        public ParcelNavigateMedia(UUI agent, SceneInterface scene)
        {
            m_Agent = agent;
            m_Scene = scene;
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

            UInt32 localID = reqmap["local-id"].AsUInt; /* this is parcel local id */
            string url = reqmap["url"].ToString();

#warning Implement ParcelNavigateMedia

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
