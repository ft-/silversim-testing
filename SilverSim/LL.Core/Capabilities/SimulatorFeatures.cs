// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Core;
using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class SimulatorFeatures : ICapabilityInterface
    {
        public readonly Map Features = new Map();

        public string CapabilityName 
        { 
            get
            {
                return "SimulatorFeatures";
            }
        }

        public SimulatorFeatures(string searchUrl, string gridName, string gridURL, bool exportSupported)
        {
            Features.Add("MeshRezEnabled", true);
            Features.Add("MeshUploadEnabled", true);
            Features.Add("MeshXferEnabled", true);
            Map typesMap = new Map();
            typesMap.Add("convex", true);
            typesMap.Add("none", true);
            typesMap.Add("prim", true);
            Features.Add("PhysicsShapeTypes", typesMap);
            Map extrasMap = new Map();
            if (!string.IsNullOrEmpty(gridURL))
            {
                extrasMap.Add("GridURL", gridURL);
            }
            if (!string.IsNullOrEmpty(gridName))
            {
                extrasMap.Add("GridName", gridName);
            }

            if(!string.IsNullOrEmpty(searchUrl))
            {
                extrasMap.Add("search-server-url", searchUrl);
            }
            if(exportSupported)
            {
                extrasMap.Add("ExportSupported", true);
            }
            if (extrasMap.Count > 0)
                Features.Add("OpenSimExtras", extrasMap);
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            HttpResponse res;
            if(httpreq.Method != "GET")
            {
                res = httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
            }
            else
            {
                res = httpreq.BeginResponse();
                res.ContentType = "application/llsd+xml";
                LLSD_XML.Serialize(Features, res.GetOutputStream());
            }

            res.Close();
        }
    }
}
