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

using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using ThreadedClasses;

namespace SilverSim.Scripting.LSL
{
    class LSLHTTP : IPlugin, IPluginShutdown
    {
        BaseHttpServer m_HttpServer;
        Timer m_HttpTimer;

        struct HttpRequestData
        {
            public DateTime ValidUntil;
            public string ContentType;
            public HttpRequest Request;
            public UUID UrlID;

            public HttpRequestData(HttpRequest req, UUID urlID)
            {
                Request = req;
                ContentType = "text/plain";
                ValidUntil = DateTime.UtcNow + TimeSpan.FromSeconds(25);
                UrlID = urlID;
            }
        }

        readonly RwLockedDictionary<UUID, HttpRequestData> m_HttpRequests = new RwLockedDictionary<UUID, HttpRequestData>();

        struct URLData
        {
            public UUID SceneID;
            public UUID PrimID;
            public UUID ItemID;

            public URLData(UUID sceneID, UUID primID, UUID itemID)
            {
                SceneID = sceneID;
                PrimID = primID;
                ItemID = itemID;
            }
        }
        readonly RwLockedDictionary<UUID, URLData> m_UrlMap = new RwLockedDictionary<UUID, URLData>();

        public LSLHTTP()
        {
            m_HttpTimer = new Timer(1000);
            m_HttpTimer.Elapsed += TimerEvent;
            m_HttpTimer.Start();
        }

        void TimerEvent(object sender, ElapsedEventArgs e)
        {
            List<UUID> RemoveList = new List<UUID>();
            foreach(KeyValuePair<UUID, HttpRequestData> kvp in m_HttpRequests)
            {
                if(kvp.Value.ValidUntil < DateTime.UtcNow)
                {
                    RemoveList.Add(kvp.Key);
                }
            }

            HttpRequestData reqdata;
            foreach(UUID id in RemoveList)
            {
                if(m_HttpRequests.Remove(id, out reqdata))
                {
                    reqdata.Request.SetConnectionClose();
                    reqdata.Request.ErrorResponse(HttpStatusCode.InternalServerError, "Script timeout");
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/lslhttp/", LSLHttpRequestHandler);
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_HttpTimer.Stop();
            m_HttpServer.StartsWithUriHandlers.Remove("/lslhttp/");

            HttpRequestData reqdata;
            foreach (UUID id in m_HttpRequests.Keys)
            {
                if (m_HttpRequests.Remove(id, out reqdata))
                {
                    reqdata.Request.SetConnectionClose();
                    reqdata.Request.ErrorResponse(HttpStatusCode.InternalServerError, "Script shutdown");
                }
            }

        }

        public void LSLHttpRequestHandler(HttpRequest req)
        {
            string[] parts = req.RawUrl.Substring(1).Split(new char[] {'/'}, 3);
            UUID id;
            URLData urlData;
            if (req.Method != "GET" && req.Method != "POST" && req.Method != "PUT" && req.Method != "DELETE")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }
            
            if (parts.Length < 2)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if(!UUID.TryParse(parts[1], out id))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if(!m_UrlMap.TryGetValue(id, out urlData))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            req["x-script-url"] = m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + m_HttpServer.Port.ToString() + "/lslhttp/" + id;
            string pathinfo = req.RawUrl.Substring(45);
            int pos = pathinfo.IndexOf('?');
            if (pos >= 0)
            {
                req["x-path-info"] = pathinfo.Substring(0, pos);
                req["x-query-string"] = req.RawUrl.Substring(pos + 1);
            }
            else
            {
                req["x-path-info"] = pathinfo;
            }
            req["x-remote-ip"] = req.CallerIP.ToString();

            UUID reqid = UUID.Random;
            HttpRequestData data = new HttpRequestData(req, id);

            string body = "";
            if(data.Request.Method != "GET" && data.Request.Method != "DELETE")
            {
                int length = int.Parse(data.Request["Content-Length"]);
                byte[] buf = new byte[length];
                data.Request.Body.Read(buf, 0, length);
                body = UTF8NoBOM.GetString(buf);
            }

            try
            {
                m_HttpRequests.Add(reqid, data);
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.InternalServerError, "script access error");
                return;
            }

            HttpRequestEvent ev = new HttpRequestEvent();
            ev.RequestID = reqid;
            ev.Body = body;
            ev.Method = data.Request.Method;

            try
            {
                SceneInterface scene = SceneManager.Scenes[urlData.SceneID];
                ObjectPart part = scene.Primitives[urlData.ItemID];
                ObjectPartInventoryItem item = part.Inventory[urlData.ItemID];
                ScriptInstance instance = item.ScriptInstance;
                if (instance == null)
                {
                    throw new ArgumentNullException();
                }
                instance.PostEvent(ev);
            }
            catch
            {
                m_HttpRequests.Remove(reqid);
                data.Request.ErrorResponse(HttpStatusCode.InternalServerError, "script access error");
                return;
            }
            throw new HttpResponse.DisconnectFromThreadException();
        }

        public string GetHttpHeader(UUID requestId, string header)
        {
            HttpRequestData reqdata;
            if (m_HttpRequests.TryGetValue(requestId, out reqdata))
            {
                if(reqdata.Request.ContainsHeader(header))
                {
                    return reqdata.Request[header];
                }
            }
            return string.Empty;
        }

        public void SetContentType(UUID requestID, string contentType)
        {
            HttpRequestData reqdata;
            if(m_HttpRequests.TryGetValue(requestID, out reqdata))
            {
                reqdata.ContentType = contentType;
            }
        }

        public void HttpResponse(UUID requestID, int status, string body)
        {
            HttpRequestData reqdata;
            if(m_HttpRequests.Remove(requestID, out reqdata))
            {
                byte[] b = UTF8NoBOM.GetBytes(body);
                HttpStatusCode httpStatus = (HttpStatusCode)status;
                reqdata.Request.SetConnectionClose();
                HttpResponse res = reqdata.Request.BeginResponse(httpStatus, httpStatus.ToString(), reqdata.ContentType);
                using(Stream s = res.GetOutputStream(b.LongLength))
                {
                    s.Write(b, 0, b.Length);
                }
                res.Close();
            }
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public string RequestURL(ObjectPart part, ObjectPartInventoryItem item)
        {
            UUID newid = UUID.Random;
            m_UrlMap.Add(newid, new URLData(part.ObjectGroup.Scene.ID, part.ID, item.ID));
            return m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + ":" + m_HttpServer.Port.ToString() + "/lslhttp/" + newid;
        }

        public string RequestSecureURL(ObjectPart part, ObjectPartInventoryItem item)
        {
            throw new NotImplementedException();
        }

        public void ReleaseURL(string url)
        {
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch
            {
                return;
            }

            string[] parts = uri.PathAndQuery.Substring(1).Split(new char[] { '/' }, 3);
            if(parts.Length < 2)
            {
                return;
            }
            else if(parts[0] != "lslhttp")
            {
                return;
            }
            
            UUID urlid;
            if(!UUID.TryParse(parts[1], out urlid))
            {
                return;
            }

            if(m_UrlMap.Remove(urlid))
            {
                List<UUID> RemoveList = new List<UUID>();
                foreach (KeyValuePair<UUID, HttpRequestData> kvp in m_HttpRequests)
                {
                    if (kvp.Value.UrlID == urlid)
                    {
                        RemoveList.Add(kvp.Key);
                    }
                }

                HttpRequestData reqdata;
                foreach (UUID id in RemoveList)
                {
                    if (m_HttpRequests.Remove(id, out reqdata))
                    {
                        reqdata.Request.SetConnectionClose();
                        reqdata.Request.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    }
                }
            }
        }
    }
}
