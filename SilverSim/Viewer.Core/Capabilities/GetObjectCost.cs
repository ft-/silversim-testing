﻿// SilverSim is distributed under the terms of the
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
using SilverSim.Types.StructuredData.Llsd;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public sealed class GetObjectCost : ICapabilityInterface
    {
        private readonly ViewerAgent m_Agent;
        private readonly SceneInterface m_Scene;
        private readonly string m_RemoteIP;

        public GetObjectCost(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName => "GetObjectCost";

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

            AnArray objectlist;
            var resdata = new Map();
            if(reqmap.TryGetValue("object_ids", out objectlist))
            {
                foreach(IValue iv in objectlist)
                {
                    UUID id = iv.AsUUID;
                    ObjectPart part;
                    if(m_Scene.Primitives.TryGetValue(id, out part))
                    {
                        resdata.Add(id.ToString(), new Map
                        {
                            { "linked_set_resource_cost", part.ObjectGroup.LinkCost },
                            { "linked_set_physics_cost", part.ObjectGroup.PhysicsCost },
                            { "resource_cost", part.LinkCost },
                            { "physics_cost", part.PhysicsCost }
                        });
                    }
                }
            }

            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            using (Stream s = res.GetOutputStream())
            {
                LlsdXml.Serialize(resdata, s);
            }
        }
    }
}
