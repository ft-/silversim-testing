// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Types.Primitive;
using System;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ObjectMediaNavigate : ICapabilityInterface
    {
        readonly UUI m_Agent;
        readonly SceneInterface m_Scene;

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

        bool CheckUrlAgainstWhiteList(string rawUrl, PrimitiveMedia.Entry entry)
        {
            Uri url = new Uri(rawUrl);

            foreach (string origWhitelistUrl in entry.WhiteList)
            {
                string whitelistUrl = origWhitelistUrl;

                /* Deal with a line-ending wildcard */
                if (whitelistUrl.EndsWith("*"))
                {
                    whitelistUrl = whitelistUrl.Remove(whitelistUrl.Length - 1);
                }

                /* A starting wildcard is only meant to match the domain not the rest */
                if (whitelistUrl.StartsWith("*"))
                {
                    whitelistUrl = whitelistUrl.Substring(1);

                    if (url.Host.Contains(whitelistUrl))
                    {
                        return true;
                    }
                }
                else
                {
                    string urlToMatch = url.Authority + url.AbsolutePath;

                    if (urlToMatch.StartsWith(whitelistUrl))
                    {
                        return true;
                    }
                }
            }

            return false;
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

            UUID objectID = reqmap["object_id"].AsUUID;
            string currentURL = reqmap["current_url"].ToString();
            int textureIndex = reqmap["texture_index"].AsInt;

            ObjectPart part = m_Scene.Primitives[objectID];
            PrimitiveMedia.Entry entry;
            try
            {
                entry = part.Media[textureIndex];
                if(null == entry)
                {
                    /* nothing to do */
                }
                else if(0 != (entry.InteractPermissions & PrimitiveMediaPermission.Anyone))
                {
                    /* permission is okay when anyone is enabled */
                }
                else if(0 != (entry.InteractPermissions & PrimitiveMediaPermission.Owner) && 
                    part.ObjectGroup.Owner.EqualsGrid(m_Agent) && 
                    !part.ObjectGroup.IsGroupOwned)
                {
                    /* permission is okay when group is allowed and agent is group member */
                }
                else if(0 == (entry.InteractPermissions & PrimitiveMediaPermission.Group))
                {
                    entry = null;
                }
                else
                {
                    GroupsServiceInterface groupsService = part.ObjectGroup.Scene.GroupsService;
                    if(null != groupsService)
                    {
                        UGI groupID = part.ObjectGroup.Group;
                        if(!groupsService.Memberships.ContainsKey(m_Agent, groupID, m_Agent))
                        {
                            entry = null;
                        }
                    }
                }
            }
            catch
            {
                entry = null;
            }

            if(entry == null)
            {
                using (HttpResponse resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
                {
                    resp.ContentType = "text/plain";
                }
                return;
            }

            if (entry.IsWhiteListEnabled && !CheckUrlAgainstWhiteList(currentURL, entry))
            {
                using (HttpResponse resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
                {
                    resp.ContentType = "text/plain";
                }
                return;
            }
            if(entry != null)
            {
                entry.CurrentURL = currentURL;
                part.UpdateMediaFace(textureIndex, entry, m_Agent.ID);
            }

            using (HttpResponse resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                resp.ContentType = "application/llsd+xml";
                using (Stream s = resp.GetOutputStream())
                {
                    LlsdXml.Serialize(new Undef(), s);
                }
            }
        }
    }
}
