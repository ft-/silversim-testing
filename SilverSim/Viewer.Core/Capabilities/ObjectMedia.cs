// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ObjectMedia : ICapabilityInterface
    {
        UUI m_Agent;
        SceneInterface m_Scene;

        public ObjectMedia(UUI agent, SceneInterface scene)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public string CapabilityName
        {
            get
            {
                return "ObjectMedia";
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
            if(!reqmap.ContainsKey("verb"))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Invalid request");
                return;
            }
            string verb = reqmap["verb"].ToString();
            switch(verb)
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

        void HandleObjectMediaRequest(HttpRequest httpreq, Map reqmap)
        {
            HttpResponse resp;
            UUID objectID = reqmap["object_id"].AsUUID;
            ObjectPart part;
            try
            {
                part = m_Scene.Primitives[objectID];
            }
            catch
            {
                resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
                resp.ContentType = "text/plain";
                resp.Close();
                return;
            }

            Map res = new Map();
            res.Add("object_id", objectID);
            PrimitiveMedia mediaList = part.Media;
            if(null == mediaList)
            {
                resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
                resp.ContentType = "text/plain";
                resp.Close();
                return;
            }
            AnArray mediaData = new AnArray();
            foreach (PrimitiveMedia.Entry entry in part.Media)
            {
                if(null != entry)
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
            resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
            resp.ContentType = "application/llsd+xml";
            using(Stream o = resp.GetOutputStream())
            {
                LLSD_XML.Serialize(res, o);
            }
            resp.Close();

        }

        void HandleObjectMediaUpdate(HttpRequest httpreq, Map reqmap)
        {
            HttpResponse resp;
            UUID objectID = reqmap["object_id"].AsUUID;
            ObjectPart part;
            try
            {
                part = m_Scene.Primitives[objectID];
            }
            catch
            {
                resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
                resp.ContentType = "text/plain";
                resp.Close();
                return;
            }

            PrimitiveMedia media = new PrimitiveMedia();
            foreach (IValue v in (AnArray)reqmap["object_media_data"])
            {
                if (v is Map)
                {
                    media.Add((PrimitiveMedia.Entry)((Map)v));
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

            resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
            resp.ContentType = "text/plain";
            resp.Close();
        }
    }
}
