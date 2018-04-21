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
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Types.StructuredData.Llsd;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class LandResources : ICapabilityInterface
    {
        private readonly SceneInterface m_Scene;
        private readonly ViewerAgent m_Agent;
        private readonly string m_RemoteIP;
        private readonly string m_ServerURI;

        public string CapabilityName => "LandResources";

        public LandResources(ViewerAgent agent, SceneInterface scene, string serverURI, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
            m_ServerURI = serverURI;
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != m_RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            var parts = httpreq.RawUrl.Substring(1).Split('/');

            if (parts.Length == 3)
            {
                if (httpreq.ContentType != "application/llsd+xml")
                {
                    httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
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
                    IValue iv = LlsdXml.Deserialize(httpreq.Body);
                    reqmap = iv as Map;
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    return;
                }
                UUID parcelID;
                if (reqmap == null || !reqmap.TryGetValue("parcel_id", out parcelID))
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                    return;
                }

                var parcelPos = new ParcelID(parcelID.GetBytes(), 0);

                ParcelInfo pinfo;
                if(!m_Scene.Parcels.TryGetValue(parcelPos.Location, out pinfo))
                {
                    httpreq.ErrorResponse(HttpStatusCode.Gone, "Gone");
                    return;
                }

                var resdata = new Map
                {
                    { "ScriptResourceDetails", m_ServerURI + httpreq.RawUrl + "/Details/" + pinfo.ID.ToString() },
                    { "ScriptResourceSummary", m_ServerURI + httpreq.RawUrl + "/Summary/" + pinfo.ID.ToString() }
                };

                using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
                using (Stream s = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, s);
                }
            }
            else if(parts.Length == 5)
            {
                UUID parcelID;
                if (!UUID.TryParse(parts[4], out parcelID))
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                    return;
                }

                if (httpreq.Method != "GET")
                {
                    httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    return;
                }

                switch(parts[3])
                {
                    case "Details":
                        HandleDetailsReport(httpreq, parcelID);
                        break;

                    case "Summary":
                        HandleSummaryReport(httpreq, parcelID);
                        break;

                    default:
                        httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                        break;
                }
            }
            else
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not found");
            }
        }

        private void HandleDetailsReport(HttpRequest httpreq, UUID parcelID)
        {
            var parcels = new AnArray();

            ParcelInfo pinfo;
            if(m_Scene.Parcels.TryGetValue(parcelID, out pinfo))
            {
                var parcelobjects = new AnArray();
                var parcel = new Map
                {
                    { "id", pinfo.ID },
                    { "local_id", pinfo.LocalID },
                    { "name", pinfo.Name },
                    { "owner_id", pinfo.Owner.ID },
                    { "objects", parcelobjects }
                };
                parcels.Add(parcel);

                foreach (ObjectGroup grp in m_Scene.ObjectGroups)
                {
                    Vector3 loc = grp.GlobalPosition;
                    int urls = 0;
                    int memory = 0;
                    if (!grp.IsAttached && pinfo.LandBitmap.ContainsLocation(loc))
                    {
                        parcelobjects.Add(new Map
                        { 
                            { "id", grp.ID },
                            { "name", grp.Name },
                            { "owner_id", grp.Owner.ID },
                            { "owner_name", m_Scene.AvatarNameService.ResolveName(grp.Owner).FullName },
                            { "location", new Map
                                {
                                    { "x", loc.X },
                                    { "y", loc.Y },
                                    { "z", loc.Z }
                                }
                            },
                            { "resources", new Map
                                {
                                    { "urls", urls },
                                    { "memory", memory }
                                }
                            }
                        });
                    }
                }
            }

            using (HttpResponse res = httpreq.BeginResponse("applicaton/llsd+xml"))
            using (Stream s = res.GetOutputStream())
            {
                LlsdXml.Serialize(new Map { { "parcels", parcels } }, s);
            }
        }

        private void HandleSummaryReport(HttpRequest httpreq, UUID parcelID)
        {
            var available = new AnArray();
            var used = new AnArray();
            var resdata = new Map
            {
                { "summary", new Map
                    {
                        { "available", available },
                        { "used", used }
                    }
                }
            };

            available.Add(new Map
            {
                { "type", "url" },
                { "amount", 5000 }
            });

            available.Add(new Map
            {
                { "type", "memory" },
                { "amount", 1048576 }
            });

            used.Add(new Map
            {
                { "type", "urls" },
                { "amount", 5000 }
            });

            used.Add(new Map
            {
                { "type", "memory" },
                { "amount", 1048576 }
            });

            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            using (Stream s = res.GetOutputStream())
            {
                LlsdXml.Serialize(resdata, s);
            }
        }
    }
}