// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Messages;
using System;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        readonly BlockingQueue<Message> m_EventQueue = new BlockingQueue<Message>();
        bool m_EventQueueEnabled = true;
        int m_EventQueueEventId = 1;

        protected override void SendViaEventQueueGet(Message m)
        {
            m_EventQueue.Enqueue(m);
        }

        void Cap_EventQueueGet(HttpRequest httpreq)
        {
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                LlsdXml.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            int timeout = 30;
            Message m = null;
            while(timeout -- != 0)
            {
                if(!m_EventQueueEnabled)
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                try
                {
                    m = m_EventQueue.Dequeue(1000);
                    break;
                }
                catch
                {
                    /* no action required */
                }
            }

            if(null == m)
            {
                using (HttpResponse res = httpreq.BeginResponse(HttpStatusCode.BadGateway, "Upstream error:"))
                {
                    res.MinorVersion = 0;
                    using (TextWriter w = res.GetOutputStream().UTF8StreamWriter())
                    {
                        w.Write("Upstream error: ");
                        w.Flush();
                    }
                }
                return;
            }

            AnArray eventarr = new AnArray();
            int count = m_EventQueue.Count - 1;

            do
            {
                IValue body;
                string message;

                try
                {
                    message = m.NameEQG;
                    body = m.SerializeEQG();
                }
                catch (Exception e)
                {
                    m_Log.DebugFormat("Unsupported message {0} in EventQueueGet: {1}\n{2}", m.GetType().FullName, e.Message, e.StackTrace);
                    using (HttpResponse res = httpreq.BeginResponse(HttpStatusCode.BadGateway, "Upstream error:"))
                    {
                        res.MinorVersion = 0;
                        using (TextWriter w = res.GetOutputStream().UTF8StreamWriter())
                        {
                            w.Write("Upstream error: ");
                            w.Flush();
                        }
                    }
                    return;
                }
                Map ev = new Map();
                ev.Add("message", message);
                ev.Add("body", body);
                eventarr.Add(ev);
                if(count > 0)
                {
                    --count;
                    m = m_EventQueue.Dequeue(0);
                }
                else
                {
                    m = null;
                }
            } while (m != null);

            Map result = new Map();
            result.Add("events", eventarr);
            result.Add("id", m_EventQueueEventId++);

            using (HttpResponse res = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                res.ContentType = "application/llsd+xml";
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(result, o);
                }
            }
        }
    }
}
