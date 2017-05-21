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
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ObjectMedia : ICapabilityInterface
    {
        private readonly UUI m_Agent;
        private readonly SceneInterface m_Scene;
        private readonly string m_RemoteIP;

        public ObjectMedia(UUI agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName => "ObjectMedia";

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

            if(!reqmap.ContainsKey("verb"))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Invalid request");
                return;
            }
            switch(reqmap["verb"].ToString())
            {
                case "GET":
                    HandleObjectMediaRequest(httpreq, reqmap);
                    break;

                case "UPDATE":
                    HandleObjectMediaUpdate(httpreq, reqmap);
                    break;

                default:
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Invalid request");
                    break;
            }
        }

        private void HandleObjectMediaRequest(HttpRequest httpreq, Map reqmap)
        {
            UUID objectID = reqmap["object_id"].AsUUID;
            ObjectPart part;
            try
            {
                part = m_Scene.Primitives[objectID];
            }
            catch
            {
                using (HttpResponse resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
                {
                    resp.ContentType = "text/plain";
                }
                return;
            }

            var res = new Map
            {
                ["object_id"] = objectID
            };
            var mediaList = part.Media;
            if(mediaList == null)
            {
                using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
                {
                    resp.ContentType = "text/plain";
                }
                return;
            }
            var mediaData = new AnArray();
            foreach (var entry in part.Media)
            {
                if(entry != null)
                {
                    mediaData.Add((Map)entry);
                }
                else
                {
                    mediaData.Add(new Undef());
                }
            }
            res.Add("object_media_data", mediaData);
            res.Add("object_media_version", part.MediaURL);
            using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                resp.ContentType = "application/llsd+xml";
                using (var o = resp.GetOutputStream())
                {
                    LlsdXml.Serialize(res, o);
                }
            }
        }

        private void HandleObjectMediaUpdate(HttpRequest httpreq, Map reqmap)
        {
            UUID objectID = reqmap["object_id"].AsUUID;
            ObjectPart part;
            try
            {
                part = m_Scene.Primitives[objectID];
            }
            catch
            {
                using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
                {
                    resp.ContentType = "text/plain";
                }
                return;
            }

            var media = new PrimitiveMedia();
            foreach (var v in (AnArray)reqmap["object_media_data"])
            {
                var vm = v as Map;
                if (vm != null)
                {
                    media.Add((PrimitiveMedia.Entry)vm);
                }
                else
                {
                    media.Add(null);
                }
            }

            if (part.CheckPermissions(m_Agent, part.ObjectGroup.Group, Types.Inventory.InventoryPermissionsMask.Modify))
            {
                part.UpdateMedia(media, m_Agent.ID);
            }

            using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                resp.ContentType = "text/plain";
            }
        }
    }
}
