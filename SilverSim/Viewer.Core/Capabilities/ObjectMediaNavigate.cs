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
using SilverSim.Scene.Types.Object;
using SilverSim.Types.Primitive;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types.Groups;
using System.IO;

namespace SilverSim.Viewer.Core.Capabilities
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
            HttpResponse resp;
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
                }
                else if(0 != (entry.InteractPermissions & PrimitiveMediaPermission.Anyone))
                {

                }
                else if(0 != (entry.InteractPermissions & PrimitiveMediaPermission.Owner) && 
                    part.ObjectGroup.Owner.EqualsGrid(m_Agent) && 
                    !part.ObjectGroup.IsGroupOwned)
                {

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
                        GroupMembership gm = groupsService.Memberships[m_Agent, groupID, m_Agent];
                    }
                }
            }
            catch
            {
                entry = null;
            }

            if(entry == null)
            {
                resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
                resp.ContentType = "text/plain";
                resp.Close();
                return;
            }

            if (entry.IsWhiteListEnabled && !CheckUrlAgainstWhiteList(currentURL, entry))
            {
                resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
                resp.ContentType = "text/plain";
                resp.Close();
                return;
            }
            if(entry != null)
            {
                entry.CurrentURL = currentURL;
                part.UpdateMediaFace(textureIndex, entry, m_Agent.ID);
            }

            resp = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
            resp.ContentType = "application/llsd+xml";
            using(Stream s = resp.GetOutputStream())
            {
                LLSD_XML.Serialize(new Undef(), s);
            }
            resp.Close();
        }
    }
}
