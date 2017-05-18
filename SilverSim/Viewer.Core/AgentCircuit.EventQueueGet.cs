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

            var timeout = 30;
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
                using (var res = httpreq.BeginResponse(HttpStatusCode.BadGateway, "Upstream error:"))
                {
                    res.MinorVersion = 0;
                    using (var w = res.GetOutputStream().UTF8StreamWriter())
                    {
                        w.Write("Upstream error: ");
                        w.Flush();
                    }
                }
                return;
            }

            var eventarr = new AnArray();
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
                    using (var res = httpreq.BeginResponse(HttpStatusCode.BadGateway, "Upstream error:"))
                    {
                        res.MinorVersion = 0;
                        using (var w = res.GetOutputStream().UTF8StreamWriter())
                        {
                            w.Write("Upstream error: ");
                            w.Flush();
                        }
                    }
                    return;
                }
                var ev = new Map
                {
                    { "message", message },
                    { "body", body }
                };
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

            var result = new Map
            {
                { "events", eventarr },
                { "id", m_EventQueueEventId++ }
            };
            using (var res = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                res.ContentType = "application/llsd+xml";
                using (var o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(result, o);
                }
            }
        }
    }
}
