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
using SilverSim.StructuredData.LLSD;
using ThreadedClasses;
using SilverSim.Scene.Types.Scene;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ParcelNavigateMedia : ICapabilityInterface
    {
        UUI m_Agent;
        SceneInterface m_Scene;

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

            IValue o;
            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (!(o is Map))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }
            Map reqmap = (Map)o;

            if(reqmap["agent-id"].AsUUID != m_Agent.ID)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            UInt32 localID = reqmap["local-id"].AsUInt; /* this is parcel local id */
            string url = reqmap["url"].ToString();

#warning Implement ParcelNavigateMedia

            Map m = new Map();
            HttpResponse resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
            resp.ContentType = "application/llsd+xml";
            LLSD_XML.Serialize(m, resp.GetOutputStream());
            resp.Close();
        }
    }
}
