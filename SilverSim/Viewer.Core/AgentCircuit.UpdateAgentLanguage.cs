// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        public void Cap_UpdateAgentLanguage(HttpRequest httpreq)
        {
            IValue o;
            if(httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LlsdXml.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }

            Map reqmap = o as Map;
            if (null == reqmap)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            string agentLanguage = reqmap["language"].ToString();
            bool isPublic = reqmap["language_is_public"].AsBoolean;
            Agent.AgentLanguage = isPublic ? agentLanguage : string.Empty;

            Map resmap = new Map();
            using (HttpResponse res = httpreq.BeginResponse())
            {
                using (Stream stream = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resmap, stream);
                }
            }

        }
    }
}
