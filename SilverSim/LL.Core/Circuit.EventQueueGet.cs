/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.LL.Messages;
using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System;
using System.IO;
using System.Net;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        BlockingQueue<Message> m_EventQueue = new BlockingQueue<Message>();
        bool m_EventQueueEnabled = true;
        int m_EventQueueEventId = 0;

        void Cap_EventQueueGet(HttpRequest httpreq)
        {
            IValue iv;
            if (httpreq.Method != "POST")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            try
            {
                iv = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.BeginResponse(HttpStatusCode.BadRequest, "Bad Request").Close();
                return;
            }

            int timeout = 30;
            Message m = null;
            HttpResponse res;
            while(timeout -- != 0)
            {
                if(!m_EventQueueEnabled)
                {
                    httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
                    return;
                }
                try
                {
                    m = m_EventQueue.Dequeue(1000);
                    break;
                }
                catch
                {
                }
            }

            if(null == m)
            {
                res = httpreq.BeginResponse(HttpStatusCode.BadGateway, "Upstream error:");
                res.MinorVersion = 0;
                using(TextWriter w = new StreamWriter(res.GetOutputStream(), UTF8NoBOM))
                {
                    w.Write("Upstream error: ");
                    w.Flush();
                }
                res.Close();
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
                    m_Log.DebugFormat("Unsupported message {0} in EventQueueGet: {1}\n{2}", m.GetType().FullName, e.Message, e.StackTrace.ToString());
                    res = httpreq.BeginResponse(HttpStatusCode.BadGateway, "Upstream error:");
                    res.MinorVersion = 0;
                    using (TextWriter w = new StreamWriter(res.GetOutputStream(), UTF8NoBOM))
                    {
                        w.Write("Upstream error: ");
                        w.Flush();
                    }
                    res.Close();
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
            result.Add("id", m_EventQueueEventId);
            result.Add("events", eventarr);

            res = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
            res.ContentType = "application/llsd+xml";
            Stream o = res.GetOutputStream();
            LLSD_XML.Serialize(result, o);
            res.Close();
        }
    }
}
