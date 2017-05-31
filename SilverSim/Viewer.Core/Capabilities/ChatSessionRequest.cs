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
using SilverSim.Scene.Types.Agent;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class ChatSessionRequest : ICapabilityInterface
    {
        private readonly ViewerAgent m_Agent;
        private readonly AgentCircuit m_Circuit;
        private readonly string m_RemoteIP;

        public readonly RwLockedDictionary<string, Func<ViewerAgent, AgentCircuit, HttpRequest, Map, Map>> ChatSessionRequestMethodHandlers = new RwLockedDictionary<string, Func<ViewerAgent, AgentCircuit, HttpRequest, Map, Map>>();

        public ChatSessionRequest(ViewerAgent agent, AgentCircuit circuit, string remoteIP)
        {
            m_Agent = agent;
            m_Circuit = circuit;
            m_RemoteIP = remoteIP;
        }

        public string CapabilityName => "ChatSessionRequest";

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

            string method;
            if (!reqmap.TryGetValue("method", out method))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Unknown method");
                return;
            }

            Func<ViewerAgent, AgentCircuit, HttpRequest, Map, Map> del;
            if(!ChatSessionRequestMethodHandlers.TryGetValue(method, out del))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Unknown method");
                return;
            }

            Map resdata = del(m_Agent, m_Circuit, httpreq, reqmap);

            using (HttpResponse httpres = httpreq.BeginResponse())
            {
                httpres.ContentType = "application/llsd+xml";
                using (Stream outStream = httpres.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, outStream);
                }
            }
        }
    }
}
