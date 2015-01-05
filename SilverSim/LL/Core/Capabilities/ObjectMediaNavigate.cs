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
using SilverSim.Scene.Types.Object;

namespace SilverSim.LL.Core.Capabilities
{
    public class ObjectMediaNavigate : ICapabilityInterface
    {
        UUI m_Agent;
        SceneInterface m_Scene;

        public ObjectMediaNavigate(UUI agent, SceneInterface scene)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public string CapabilityName
        {
            get
            {
                return "ObjectMediaNavigate";
            }
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.Method != "POST")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            IValue o;
            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch
            {
                httpreq.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type").Close();
                return;
            }
            if (!(o is Map))
            {
                httpreq.BeginResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML").Close();
                return;
            }
            Map reqmap = (Map)o;
            UUID objectID = reqmap["object_id"].AsUUID;
            string currentURL = reqmap["current_url"].ToString();
            int textureIndex = reqmap["texture_index"].AsInt;

            ObjectPart part = m_Scene.Primitives[objectID];

#warning Implement ObjectMediaNavigate

            Map e = new Map();
            HttpResponse resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
            resp.ContentType = "text/plain";
            resp.Close();
        }
    }
}
