﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using ThreadedClasses;

namespace SilverSim.WebIF.Admin
{
    #region Service Implementation
    [Description("Administration Web-Interface")]
    public class AdminWebIF : IPlugin, IPluginShutdown
    {
        ServerParamServiceInterface m_ServerParams;
        BaseHttpServer m_HttpServer;
        BaseHttpServer m_HttpsServer;
        readonly string m_BasePath;
        const string JsonContentType = "application/json";

        class SessionInfo
        {
            public int LastSeenTickCount;
            public bool IsAuthenticated;
            public string UserName = string.Empty;
            public string ExpectedResponse = string.Empty;
            public List<string> Rights = new List<string>();

            public SessionInfo()
            {
                LastSeenTickCount = Environment.TickCount;
            }
        }

        readonly RwLockedDictionary<string, SessionInfo> m_Sessions = new RwLockedDictionary<string, SessionInfo>();
        readonly Timer m_Timer = new Timer(1);

        #region Helpers
        public static void SuccessResponse(HttpRequest req, Map m)
        {
            m.Add("success", true);
            using (HttpResponse res = req.BeginResponse(JsonContentType))
            {
                using (Stream o = res.GetOutputStream())
                {
                    Json.Serialize(m, o);
                }
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class RequiredRightAttribute : Attribute
        {
            public string Right { get; private set; }

            public RequiredRightAttribute(string right)
            {
                Right = right;
            }
        }

        public enum ErrorResult
        {
            NotLoggedIn = 1,
            NotFound = 2,
            InsufficientRights = 3,
            InvalidRequest = 4,
            AlreadyExists = 5,
            NotPossible = 6,
            InUse = 7,
            MissingSessionId = 8,
            MissingMethod = 9,
            InvalidSession = 10,
            InvalidUserAndOrPassword = 11
        };

        public static void ErrorResponse(HttpRequest req, ErrorResult reason)
        {
            Map m = new Map();
            m.Add("success", false);
            m.Add("reason", (int)reason);
            using (HttpResponse res = req.BeginResponse(JsonContentType))
            {
                using (Stream o = res.GetOutputStream())
                {
                    Json.Serialize(m, o);
                }
            }
        }
        #endregion

        public AdminWebIF(string basepath)
        {
            m_BasePath = basepath;
            m_Timer.Elapsed += HandleTimer;
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        void HandleTimer(object o, EventArgs args)
        {
            List<string> removeList = new List<string>();
            foreach(KeyValuePair<string, SessionInfo> kvp in m_Sessions)
            {
                if(Environment.TickCount - kvp.Value.LastSeenTickCount > 1000 * 600) /* 10 minutes */
                {
                    removeList.Add(kvp.Key);
                }
            }

            foreach(string sessionKey in removeList)
            {
                m_Sessions.Remove(sessionKey);
            }
        }

        public readonly RwLockedDictionary<string, Action<HttpRequest, Map>> JsonMethods = new RwLockedDictionary<string, Action<HttpRequest, Map>>();

        public void Startup(ConfigurationLoader loader)
        {
            m_ServerParams = loader.GetServerParamStorage();
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/admin", HandleUnsecureHttp);
            try
            {
                m_HttpsServer = loader.HttpsServer;
            }
            catch(ConfigurationLoader.ServiceNotFoundException)
            {
                m_HttpsServer = null;
            }

            if(null != m_HttpsServer)
            {
                m_HttpsServer.StartsWithUriHandlers.Add("/admin", HandleHttp);
            }
        }

        public void Shutdown()
        {
            JsonMethods.Clear();
            m_HttpServer.StartsWithUriHandlers.Remove("/admin");
            if (null != m_HttpsServer)
            {
                m_HttpsServer.StartsWithUriHandlers.Remove("/admin");
            }
            m_Timer.Elapsed -= HandleTimer;
            m_Timer.Dispose();
        }

        void FindUser(SessionInfo sessionInfo, UUID challenge)
        {
            string userRef = "WebIF.Admin.User." + sessionInfo.UserName + ".";
            string passmd5;
            string rights;

            if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out passmd5) &&
                m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
            {
                using (SHA1 md5 = SHA1.Create())
                {
                    byte[] str = UTF8NoBOM.GetBytes(challenge.ToString().ToLower() + "+" + passmd5.ToLower());

                    sessionInfo.ExpectedResponse = BitConverter.ToString(md5.ComputeHash(str)).Replace("-", "");
                    sessionInfo.Rights = new List<string>(rights.Split(','));
                }
            }
        }

        static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public void HandleUnsecureHttp(HttpRequest req)
        {
            if(null == m_HttpsServer || m_ServerParams.GetBoolean(UUID.Zero, "WebIF.Admin.EnableHTTP", true))
            {
                HandleHttp(req);
            }
            else
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Not Allowed");
            }
        }

        public void HandleLoginRequest(HttpRequest req, Map jsonreq)
        {
            if(!jsonreq.ContainsKey("sessionid") || !jsonreq.ContainsKey("user") || !jsonreq.ContainsKey("response"))
            {
                ErrorResponse(req, ErrorResult.InvalidRequest);
                return;
            }

            string sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
            SessionInfo sessionInfo;
            Map res = new Map();
            if(!m_Sessions.TryGetValue(sessionKey, out sessionInfo) || sessionInfo.ExpectedResponse.Length == 0)
            {
                res.Add("success", false);
                res.Add("reason", (int)ErrorResult.InvalidSession);
            }
            else if(sessionInfo.IsAuthenticated)
            {
                res.Add("success", false);
                res.Add("reason", (int)ErrorResult.InvalidSession);
            }
            else
            {
                if(jsonreq["response"].ToString() == sessionInfo.ExpectedResponse)
                {
                    sessionInfo.LastSeenTickCount = Environment.TickCount;
                    sessionInfo.IsAuthenticated = true;
                    sessionInfo.ExpectedResponse = string.Empty;
                    res.Add("success", true);
                    AnArray rights = new AnArray();
                    foreach(string right in sessionInfo.Rights)
                    {
                        rights.Add(right);
                    }
                    res.Add("rights", rights);
                }
                else
                {
                    res.Add("success", false);
                    res.Add("reason", (int)ErrorResult.InvalidUserAndOrPassword);
                    m_Sessions.Remove(sessionKey);
                }
            }
            using (HttpResponse httpres = req.BeginResponse(JsonContentType))
            {
                using (Stream o = httpres.GetOutputStream())
                {
                    Json.Serialize(res, o);
                }
            }
        }

        public void HandleChallengeRequest(HttpRequest req, Map jsonreq)
        {
            Map resdata = new Map();
            UUID sessionID = UUID.Random;
            UUID challenge = UUID.Random;
            resdata["sessionid"] = sessionID;
            resdata["challenge"] = challenge;

            if(!jsonreq.ContainsKey("user"))
            {
                ErrorResponse(req, ErrorResult.InvalidRequest);
                return;
            }

            SessionInfo sessionInfo = new SessionInfo();
            m_Sessions.Add(req.CallerIP + "+" + sessionID.ToString(), sessionInfo);
            sessionInfo.UserName = jsonreq["user"].ToString();


            using (HttpResponse res = req.BeginResponse(JsonContentType))
            {
                using (Stream o = res.GetOutputStream())
                {
                    Json.Serialize(resdata, o);
                }
            }
        }

        public void HandleHttp(HttpRequest req)
        {
            if(req.RawUrl.StartsWith("/admin/json"))
            {
                if(req.Method != "POST")
                {
                    req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                }
                else if(req.ContentType != JsonContentType)
                {
                    req.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported media type " + req.ContentType);
                }
                else
                {
                    SessionInfo sessionInfo;
                    Map jsondata;
                    try
                    {
                        jsondata = Json.Deserialize(req.Body) as Map;
                    }
                    catch(Json.InvalidJsonSerializationException)
                    {
                        req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                        return;
                    }
                    if(jsondata == null)
                    {
                        ErrorResponse(req, ErrorResult.InvalidRequest);
                        return;
                    }
                    Action<HttpRequest, Map> del;
                    if (!jsondata.ContainsKey("method"))
                    {
                        ErrorResponse(req, ErrorResult.MissingMethod);
                        return;
                    }

                    string methodName = jsondata["method"].ToString();
                    string sessionKey;

                    switch(methodName)
                    {
                        case "login":
                            HandleLoginRequest(req, jsondata);
                            break;

                        case "challenge":
                            HandleChallengeRequest(req, jsondata);
                            break;

                        default:
                            if(!jsondata.ContainsKey("sessionid"))
                            {
                                ErrorResponse(req, ErrorResult.MissingSessionId);
                                req.ErrorResponse(HttpStatusCode.BadRequest, "'sessionid' missing");
                                return;
                            }
                            sessionKey = req.CallerIP + "+" + jsondata["sessionid"].ToString();
                            if (!m_Sessions.TryGetValue(sessionKey, out sessionInfo) ||
                                !sessionInfo.IsAuthenticated)
                            {
                                ErrorResponse(req, ErrorResult.NotLoggedIn);
                                return;
                            }
                            else
                            {
                                sessionInfo.LastSeenTickCount = Environment.TickCount;
                            }
                            if (methodName == "logout")
                            {
                                m_Sessions.Remove(sessionKey);
                                using (HttpResponse res = req.BeginResponse("text/plain"))
                                {
                                    using (Stream o = res.GetOutputStream())
                                    {
                                        Map m = new Map();
                                        m.Add("success", true);
                                        Json.Serialize(m, o);
                                    }
                                }
                            }
                            else if (!JsonMethods.TryGetValue(methodName, out del))
                            {
                                req.ErrorResponse(HttpStatusCode.BadRequest, "Request method \"" + methodName + "\" is not supported.");
                                return;
                            }
                            else
                            {
                                RequiredRightAttribute attr = Attribute.GetCustomAttribute(del.GetType(), typeof(RequiredRightAttribute)) as RequiredRightAttribute;
                                if(attr != null)
                                {
                                    if(!sessionInfo.Rights.Contains(attr.Right))
                                    {
                                        ErrorResponse(req, ErrorResult.InsufficientRights);
                                        return;
                                    }
                                }
                                del(req, jsondata);
                            }
                            break;
                    }
                }
            }
            else if(req.RawUrl.StartsWith("/admin/js/") || req.RawUrl.StartsWith("/admin/css/"))
            {
                string uri = Uri.UnescapeDataString(req.RawUrl).Substring(6);
                if (uri.Contains("..") || uri.Contains("/./") || uri.Contains("\\"))
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "File Not Found");
                    return;
                }
                ServeFile(req, uri);
            }
            else
            {
                string uri = Uri.UnescapeDataString(req.RawUrl).Substring(6);
                if (uri.Contains("..") || uri.Contains("/./") || uri.Contains("\\"))
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "File Not Found");
                    return;
                }
                if (0 == uri.Length || uri.EndsWith("/"))
                {
                    uri = "index.html";
                }

                ServeFile(req, m_BasePath + uri);
            }
        }

        void ServeFile(HttpRequest req, string filepath)
        {
            try
            {
                using (FileStream file = new FileStream("../data/adminpages/" + filepath, FileMode.Open))
                {
                    string contentType = "application/octet-stream";
                    if (filepath.EndsWith(".html") || filepath.EndsWith(".htm"))
                    {
                        contentType = "text/html";
                    }
                    else if (filepath.EndsWith(".js"))
                    {
                        contentType = "text/javascript";
                    }
                    else if (filepath.EndsWith(".jpg") || filepath.EndsWith(".jpeg"))
                    {
                        contentType = "image/jpeg";
                    }
                    else if (filepath.EndsWith(".png"))
                    {
                        contentType = "image/png";
                    }
                    else if (filepath.EndsWith(".gif"))
                    {
                        contentType = "image/gif";
                    }

                    using (HttpResponse res = req.BeginResponse(contentType))
                    {
                        using (Stream o = res.GetOutputStream(file.Length))
                        {
                            byte[] buf = new byte[10240];
                            int blocklen;
                            for (blocklen = file.Read(buf, 0, 10240); 
                                0 != blocklen; 
                                blocklen = file.Read(buf, 0, 10240))
                            {
                                o.Write(buf, 0, blocklen);
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                using (HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, "Not Found"))
                {
                    res.ContentType = "text/html";
                    using (Stream o = res.GetOutputStream())
                    {
                        using (StreamWriter w = new StreamWriter(o, UTF8NoBOM))
                        {
                            w.Write("<html><head><title>Not Found</title><body>Not Found</body></html>");
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("AdminWebIF")]
    public class AdminWebIFFactory : IPluginFactory
    {
        public AdminWebIFFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new AdminWebIF(ownSection.GetString("BasePath", ""));
        }
    }
    #endregion
}
