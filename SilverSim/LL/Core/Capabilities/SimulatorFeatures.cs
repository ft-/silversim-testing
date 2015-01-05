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

using SilverSim.LL.Core;
using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System.Net;

namespace SilverSim.LL.Core.Capabilities
{
    public class SimulatorFeatures : ICapabilityInterface
    {
        public static readonly Map Features = new Map();

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
