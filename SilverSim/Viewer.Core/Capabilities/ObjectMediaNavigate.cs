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
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ObjectMediaNavigate : ICapabilityInterface
    {
        private readonly ViewerAgent m_Agent;
        private readonly SceneInterface m_Scene;
        private readonly string m_RemoteIP;

        public ObjectMediaNavigate(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName => "ObjectMediaNavigate";

        private bool CheckUrlAgainstWhiteList(string rawUrl, PrimitiveMedia.Entry entry)
        {
            var url = new Uri(rawUrl);

            foreach (var origWhitelistUrl in entry.WhiteList)
            {
                var whitelistUrl = origWhitelistUrl;

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
                    var urlToMatch = url.Authority + url.AbsolutePath;

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

            var objectID = reqmap["object_id"].AsUUID;
            var currentURL = reqmap["current_url"].ToString();
            var textureIndex = reqmap["texture_index"].AsInt;

            var part = m_Scene.Primitives[objectID];
            PrimitiveMedia.Entry entry;
            try
            {
                entry = part.Media[textureIndex];
                if(entry == null)
                {
                    /* nothing to do */
                }
                else if(0 != (entry.InteractPermissions & PrimitiveMediaPermission.Anyone))
                {
                    /* permission is okay when anyone is enabled */
                }
                else if(0 != (entry.InteractPermissions & PrimitiveMediaPermission.Owner) &&
                    part.ObjectGroup.Owner.EqualsGrid(m_Agent.Owner) &&
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
                    var groupsService = part.ObjectGroup.Scene.GroupsService;
                    if(groupsService != null)
                    {
                        UGI groupID = part.ObjectGroup.Group;
                        if(!groupsService.Memberships.ContainsKey(m_Agent.Owner, groupID, m_Agent.Owner))
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
                using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
                {
                    resp.ContentType = "text/plain";
                }
                return;
            }

            if (entry.IsWhiteListEnabled && !CheckUrlAgainstWhiteList(currentURL, entry))
            {
                using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
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

            using (var resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                resp.ContentType = "application/llsd+xml";
                using (var s = resp.GetOutputStream())
                {
                    LlsdXml.Serialize(new Undef(), s);
                }
            }
        }
    }
}
