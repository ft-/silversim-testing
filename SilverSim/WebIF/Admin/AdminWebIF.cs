// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using log4net.Core;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Main.Common.Log;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Timers;

namespace SilverSim.WebIF.Admin
{
    #region Service Implementation
    [Description("Administration Web-Interface (WebIF)")]
    public class AdminWebIF : IPlugin, IPluginShutdown, IPostLoadStep, IAdminWebIF
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ADMIN WEB IF");

        ServerParamServiceInterface m_ServerParams;
        BaseHttpServer m_HttpServer;
        BaseHttpServer m_HttpsServer;
        readonly string m_BasePath;
        const string JsonContentType = "application/json";
        RwLockedList<string> m_KnownConfigurationIssues;
        ConfigurationLoader m_Loader;
        AggregatingAvatarNameService m_AvatarNameService;
        readonly string m_AvatarNameServiceNames;
        readonly string m_Title;
        readonly BlockingQueue<LoggingEvent> m_LogEventQueue = new BlockingQueue<LoggingEvent>();

        class SessionInfo
        {
            public int LastSeenTickCount;
            public bool IsAuthenticated;
            public string UserName = string.Empty;
            public string ExpectedResponse = string.Empty;
            public List<string> Rights = new List<string>();
            public UUID SelectedSceneID = UUID.Zero;

            public SessionInfo()
            {
                LastSeenTickCount = Environment.TickCount;
                IsAuthenticated = false;
            }
        }

        readonly RwLockedDictionary<string, SessionInfo> m_Sessions = new RwLockedDictionary<string, SessionInfo>();
        readonly public RwLockedDictionaryAutoAdd<string, RwLockedList<string>> m_AutoGrantRights = new RwLockedDictionaryAutoAdd<string, RwLockedList<string>>(delegate () { return new RwLockedList<string>(); });
        public readonly RwLockedDictionary<string, Action<HttpRequest, Map>> m_JsonMethods = new RwLockedDictionary<string, Action<HttpRequest, Map>>();
        public readonly RwLockedList<string> m_Modules = new RwLockedList<string>();
        public readonly RwLockedList<HttpWebSocket> m_LogPlainReceivers = new RwLockedList<HttpWebSocket>();
        public readonly RwLockedList<HttpWebSocket> m_LogHtmlReceivers = new RwLockedList<HttpWebSocket>();

        public RwLockedDictionaryAutoAdd<string, RwLockedList<string>> AutoGrantRights
        {
            get
            {
                return m_AutoGrantRights;
            }
        }

        public RwLockedDictionary<string, Action<HttpRequest, Map>> JsonMethods
        {
            get
            {
                return m_JsonMethods;
            }
        }

        public RwLockedList<string> ModuleNames
        {
            get
            {
                return m_Modules;
            }
        }

        readonly Timer m_Timer = new Timer(1);
        readonly bool m_EnableSetPasswordCommand;

        bool m_ShutdownHandlerThreads;

        private static readonly string[] LogColors =
        {
            "blue",
            "green",
            "cyan",
            "magenta",
            "yellow"
        };

        void LogThread()
        {
            while(!m_ShutdownHandlerThreads)
            {
                LoggingEvent logevent;
                try
                {
                    logevent = m_LogEventQueue.Dequeue(500);
                }
                catch
                {
                    continue;
                }

                List<HttpWebSocket> plaintext_conns = new List<HttpWebSocket>();
                m_LogPlainReceivers.ForEach(delegate (HttpWebSocket sock)
                {
                    plaintext_conns.Add(sock);
                });

                List<HttpWebSocket> html_conns = new List<HttpWebSocket>();
                m_LogHtmlReceivers.ForEach(delegate (HttpWebSocket sock)
                {
                    html_conns.Add(sock);
                });

                string colorbegin = string.Empty;
                string colorend = string.Empty;
                string fullcolorbegin = string.Empty;
                string fullcolorend = string.Empty;
                if(logevent.Level == Level.Error)
                {
                    fullcolorbegin = "<span style=\"color: red;\">";
                    fullcolorend = "</span>";
                }
                else if(logevent.Level == Level.Warn)
                {
                    fullcolorbegin = "<span style=\"color: yellow;\">";
                    fullcolorend = "</span>";
                }
                else
                {
                    colorbegin = "<span style=\"color: " + LogColors[(Math.Abs(logevent.LoggerName.ToUpper().GetHashCode()) % LogColors.Length)] + "\">";
                    colorend = "</span>";
                }
                string msg = string.Format("{0}{1} - [{2}{3}{4}]: {5}{6}",
                    fullcolorbegin,
                    System.Web.HttpUtility.HtmlEncode(logevent.TimeStamp.ToString()),
                    colorbegin,
                    System.Web.HttpUtility.HtmlEncode(logevent.LoggerName),
                    colorend,
                    System.Web.HttpUtility.HtmlEncode(logevent.RenderedMessage.ToString()),
                    fullcolorend);
                foreach(HttpWebSocket conn in html_conns)
                {
                    try
                    {
                        conn.WriteText(msg);
                    }
                    catch
                    {
                        /* intentionally ignored */
                    }
                }
                msg = string.Format("{0} - {1:-5} [{2}]: {3}",
                    logevent.TimeStamp.ToString(),
                    logevent.Level.Name,
                    logevent.LoggerName,
                    logevent.RenderedMessage.ToString());
                foreach (HttpWebSocket conn in plaintext_conns)
                {
                    try
                    {
                        conn.WriteText(msg);
                    }
                    catch
                    {
                        /* intentionally ignored */
                    }
                }
            }
        }

        #region Helpers
        public void SuccessResponse(HttpRequest req, Map m)
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

        public void ErrorResponse(HttpRequest req, AdminWebIfErrorResult reason)
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

        public AdminWebIF(string basepath, bool enablesetpasscommand, string avatarnameservicenames, string title)
        {
            m_AvatarNameServiceNames = avatarnameservicenames;
            m_EnableSetPasswordCommand = enablesetpasscommand;
            m_Title = title;
            m_BasePath = basepath;
            m_Timer.Elapsed += HandleTimer;
            JsonMethods.Add("webif.admin.user.grantright", GrantRight);
            JsonMethods.Add("webif.admin.user.revokeright", RevokeRight);
            JsonMethods.Add("webif.admin.user.delete", DeleteUser);
            JsonMethods.Add("session.validate", HandleSessionValidateRequest);
            JsonMethods.Add("serverparam.get", GetServerParam);
            JsonMethods.Add("serverparams.get", GetServerParams);
            JsonMethods.Add("serverparams.show", ShowServerParams);
            JsonMethods.Add("serverparam.set", SetServerParam);
            JsonMethods.Add("issues.get", IssuesView);
            JsonMethods.Add("modules.list", ModulesList);
            JsonMethods.Add("module.get", ModuleGet);
            JsonMethods.Add("dnscache.list", DnsCacheList);
            JsonMethods.Add("dnscache.delete", DnsCacheRemove);
            JsonMethods.Add("webif.modules", AvailableModulesList);
            LogController.Queues.Add(m_LogEventQueue);
            new System.Threading.Thread(LogThread).Start();
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        public UUI ResolveName(UUI resolveuui)
        {
            UUI uui = resolveuui;
            UUI resultUui = uui;
            if (m_AvatarNameService.TryGetValue(uui, out resultUui))
            {
                uui = resultUui;
            }
            return uui;
        }

        public bool TranslateToUUI(string arg, out UUI uui)
        {
            return m_AvatarNameService.TranslateToUUI(arg, out uui);
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

        const string AdminUserReference = "WebIF.Admin.User.admin.";

        #region Initialization
        public void Startup(ConfigurationLoader loader)
        {
            m_Loader = loader;
            RwLockedList<AvatarNameServiceInterface> avatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
            if (!string.IsNullOrEmpty(m_AvatarNameServiceNames))
            {
                foreach (string p in m_AvatarNameServiceNames.Split(','))
                {
                    avatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(p.Trim()));
                }
            }
            m_AvatarNameService = new AggregatingAvatarNameService(avatarNameServices);

            m_KnownConfigurationIssues = loader.KnownConfigurationIssues;
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
            loader.CommandRegistry.Commands.Add("admin-webif", AdminWebIFCmd);

            if (m_HttpsServer != null && m_ServerParams.GetBoolean(UUID.Zero, "WebIF.Admin.EnableHTTP", false))
            {
                loader.KnownConfigurationIssues.Add("Set WebIF.Admin.EnableHTTP=false in section [WebIF]");
            }
        }

        public void PostLoad()
        {
            string res;
            if (!m_ServerParams.TryGetValue(UUID.Zero, AdminUserReference + "PassCode", out res))
            {
                res = UUID.Random.ToString();
                m_Log.InfoFormat("<<admin>> created password: {0}", res);
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] str = res.ToUTF8Bytes();

                    res = BitConverter.ToString(sha1.ComputeHash(str)).Replace("-", "").ToLower();
                }
                m_ServerParams[UUID.Zero, AdminUserReference + "PassCode"] = res;
                m_ServerParams[UUID.Zero, AdminUserReference + "Rights"] = "admin.all";
            }
        }

        public void Shutdown()
        {
            LogController.Queues.Remove(m_LogEventQueue);
            m_ShutdownHandlerThreads = true;
            m_Loader = null;
            JsonMethods.Clear();
            m_HttpServer.StartsWithUriHandlers.Remove("/admin");
            if (null != m_HttpsServer)
            {
                m_HttpsServer.StartsWithUriHandlers.Remove("/admin");
            }
            m_Timer.Elapsed -= HandleTimer;
            m_Timer.Dispose();
        }
        #endregion

        #region User Logic
        bool SetUserPassword(string user, string pass)
        {
            string userRef = "WebIF.Admin.User." + user + ".PassCode";
            string oldPass;
            if (m_ServerParams.TryGetValue(UUID.Zero, userRef, out oldPass))
            {
                string res;
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] str = pass.ToUTF8Bytes();

                    res = BitConverter.ToString(sha1.ComputeHash(str)).Replace("-", "").ToLower();
                }

                if (res != oldPass)
                {
                    m_ServerParams[UUID.Zero, AdminUserReference + "PassCode"] = res;
                }
                return true;
            }
            return false;
        }
        void FindUser(SessionInfo sessionInfo, UUID challenge)
        {
            string userRef = "WebIF.Admin.User." + sessionInfo.UserName + ".";
            string pass_sha1;
            string rights;

            if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
            {
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] str = (challenge.ToString().ToLower() + "+" + pass_sha1.ToLower()).ToUTF8Bytes();

                    sessionInfo.ExpectedResponse = BitConverter.ToString(sha1.ComputeHash(str)).Replace("-", "").ToLower();
                    sessionInfo.Rights = new List<string>(rights.ToLower().Split(','));
                    foreach (string r in sessionInfo.Rights.ToArray())
                    {
                        RwLockedList<string> autoGrantRightsOnRight;
                        if (AutoGrantRights.TryGetValue(r, out autoGrantRightsOnRight))
                        {
                            foreach (string g in autoGrantRightsOnRight)
                            {
                                if (!sessionInfo.Rights.Contains(g))
                                {
                                    sessionInfo.Rights.Add(g);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Core Json API handler (login, challenge, logout + method lookup)
        public void HandleUnsecureHttp(HttpRequest req)
        {
            if(null == m_HttpsServer || m_ServerParams.GetBoolean(UUID.Zero, "WebIF.Admin.EnableHTTP", m_HttpsServer == null))
            {
                HandleHttp(req);
            }
            else
            {
                using (HttpResponse res = req.BeginResponse(HttpStatusCode.MovedPermanently, "Moved Permanently"))
                {
                    res.Headers.Add("Location", m_HttpsServer.ServerURI + "admin");
                }
            }
        }

        void HandleSessionValidateRequest(HttpRequest req, Map jsonreq)
        {
            SuccessResponse(req, new Map());
        }

        void HandleLoginRequest(HttpRequest req, Map jsonreq)
        {
            if(!jsonreq.ContainsKey("sessionid") || !jsonreq.ContainsKey("user") || !jsonreq.ContainsKey("response"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            string sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
            SessionInfo sessionInfo;
            if(!m_Sessions.TryGetValue(sessionKey, out sessionInfo) || sessionInfo.IsAuthenticated)
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidSession);
            }
            else if(sessionInfo.ExpectedResponse.Length == 0)
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidUserAndOrPassword);
            }
            else
            {
                if(jsonreq["response"].ToString().ToLower() == sessionInfo.ExpectedResponse)
                {
                    sessionInfo.LastSeenTickCount = Environment.TickCount;
                    sessionInfo.IsAuthenticated = true;
                    sessionInfo.ExpectedResponse = string.Empty;
                    Map res = new Map();
                    AnArray rights = new AnArray();
                    foreach(string right in sessionInfo.Rights)
                    {
                        rights.Add(right);
                    }
                    res.Add("rights", rights);
                    res.Add("success", true);
                    if (sessionInfo.Rights.Contains("admin.all") ||
                        sessionInfo.Rights.Contains("issues.view"))
                    {
                        res.Add("numissues", m_KnownConfigurationIssues.Count);
                    }
                    using (HttpResponse httpres = req.BeginResponse(JsonContentType))
                    {
                        httpres.Headers["Set-Cookie"] = "sessionid=" + jsonreq["sessionid"].ToString() +";path=/admin";
                        using (Stream o = httpres.GetOutputStream())
                        {
                            Json.Serialize(res, o);
                        }
                    }
                }
                else
                {
                    m_Sessions.Remove(sessionKey);
                    ErrorResponse(req, AdminWebIfErrorResult.InvalidUserAndOrPassword);
                }
            }
        }

        public UUID GetSelectedRegion(HttpRequest req, Map jsonreq)
        {
            string sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
            SessionInfo info;
            if(m_Sessions.TryGetValue(sessionKey,out info))
            {
                return info.SelectedSceneID;
            }
            return UUID.Zero;
        }
        
        public void SetSelectedRegion(HttpRequest req, Map jsonreq, UUID sceneID)
        {
            string sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
            SessionInfo info;
            if (m_Sessions.TryGetValue(sessionKey, out info))
            {
                info.SelectedSceneID = sceneID;
            }
        }

        void HandleChallengeRequest(HttpRequest req, Map jsonreq)
        {
            Map resdata = new Map();
            UUID sessionID = UUID.Random;
            UUID challenge = UUID.Random;
            resdata["sessionid"] = sessionID;
            resdata["challenge"] = challenge;

            if(!jsonreq.ContainsKey("user"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            SessionInfo sessionInfo = new SessionInfo();
            m_Sessions.Add(req.CallerIP + "+" + sessionID.ToString(), sessionInfo);
            sessionInfo.UserName = jsonreq["user"].ToString().ToLower();
            FindUser(sessionInfo, challenge);
            SuccessResponse(req, resdata);
        }

        sealed class WebSocketTTY : TTY, IDisposable
        {
            readonly HttpWebSocket m_Socket;
            public WebSocketTTY(HttpWebSocket sock)
            {
                m_Socket = sock;
            }

            public void Dispose()
            {
                m_Socket.Dispose();
            }

            public void Close(HttpWebSocket.CloseReason reason)
            {
                m_Socket.Close(reason);
            }

            public override void Write(string text)
            {
                m_Socket.WriteText(text + "\n");
            }

            void DisableEcho()
            {
                byte[] echoOff = "disable echo".ToUTF8Bytes();
                m_Socket.WriteBinary(echoOff, 0, echoOff.Length);
            }

            void EnableEcho()
            {
                byte[] echoOn = "enable echo".ToUTF8Bytes();
                m_Socket.WriteBinary(echoOn, 0, echoOn.Length);
            }

            public override string ReadLine(string p, bool echoInput)
            {
                StringBuilder text = new StringBuilder();
                if(!echoInput)
                {
                    DisableEcho();
                }
                if (!string.IsNullOrEmpty(p))
                {
                    Write(p);
                }
                HttpWebSocket.Message msg;
                try
                {
                    do
                    {
                    redo:
                        try
                        {
                            msg = m_Socket.Receive();
                        }
                        catch (HttpWebSocket.MessageTimeoutException)
                        {
                            /* only ignore this if text is not set to anything yet */
                            if (text.Length == 0)
                            {
                                throw;
                            }
                            goto redo;
                        }
                        if (msg.Type == HttpWebSocket.MessageType.Text)
                        {
                            text.Append(msg.Data.FromUTF8Bytes());
                        }
                    } while (!msg.IsLastSegment || msg.Type != HttpWebSocket.MessageType.Text);
                    if (!echoInput)
                    {
                        EnableEcho();
                    }
                }
                finally
                {
                    if (!echoInput)
                    {
                        EnableEcho();
                    }
                }
                return text.ToString();
            }
        }

        void HandleWebSocketConsole(HttpRequest req, string sessionid)
        {
            Map jsondata = new Map();
            jsondata.Add("sessionid", sessionid);
            using (WebSocketTTY tty = new WebSocketTTY(req.BeginWebSocket("console")))
            {
                while(!m_ShutdownHandlerThreads)
                {
                    string cmd;
                    try
                    {
                        cmd = tty.ReadLine(string.Empty, false);
                    }
                    catch (HttpWebSocket.MessageTimeoutException)
                    {
                        continue;
                    }
                    tty.SelectedScene = GetSelectedRegion(req, jsondata);
                    m_Loader.CommandRegistry.ExecuteCommand(tty.GetCmdLine(cmd), tty);
                    SetSelectedRegion(req, jsondata, tty.SelectedScene);
                }
                if (m_ShutdownHandlerThreads)
                {
                    tty.Close(HttpWebSocket.CloseReason.GoingAway);
                }
            }
        }

        void HandleWebSocketLog(HttpRequest req, RwLockedList<HttpWebSocket> logreceivers)
        {
            using (HttpWebSocket sock = req.BeginWebSocket("log"))
            {
                sock.WriteText("Active");
                logreceivers.Add(sock);
                try
                {
                    while (!m_ShutdownHandlerThreads)
                    {
                        try
                        {
                            sock.Receive();
                        }
                        catch (HttpWebSocket.MessageTimeoutException)
                        {
                            /* ignore this exception */
                        }
                    }
                    if (m_ShutdownHandlerThreads)
                    {
                        sock.Close(HttpWebSocket.CloseReason.GoingAway);
                    }
                }
                catch (WebSocketClosedException)
                {

                }
                catch (Exception e)
                {
                    m_Log.Error("Exception during providing real-time log data", e);
                }
                finally
                {
                    logreceivers.Remove(sock);
                }
            }
        }

        public void HandleHttp(HttpRequest req)
        {
            try
            {
                if(req.RawUrl == "/admin/log" || req.RawUrl == "/admin/console" || req.RawUrl == "/admin/loghtml")
                {
                    req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                    return;
                }
                else if(req.RawUrl.StartsWith("/admin/log/"))
                {
                    if (!req.IsWebSocket)
                    {
                        req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    }

                    SessionInfo sessionInfo;
                    string sessionKey = req.CallerIP + "+" + req.RawUrl.Substring(11);
                    if (!m_Sessions.TryGetValue(sessionKey, out sessionInfo) ||
                                    !sessionInfo.IsAuthenticated ||
                                    (!sessionInfo.Rights.Contains("log.view") &&
                                    !sessionInfo.Rights.Contains("admin.all")))
                    {
                        req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                        return;
                    }
                    else
                    {
                        sessionInfo.LastSeenTickCount = Environment.TickCount;
                    }

                    HandleWebSocketLog(req, m_LogPlainReceivers);
                }
                else if (req.RawUrl.StartsWith("/admin/loghtml/"))
                {
                    if (!req.IsWebSocket)
                    {
                        req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                        return;
                    }

                    SessionInfo sessionInfo;
                    string sessionKey = req.CallerIP + "+" + req.RawUrl.Substring(15);
                    if (!m_Sessions.TryGetValue(sessionKey, out sessionInfo) ||
                                    !sessionInfo.IsAuthenticated ||
                                    (!sessionInfo.Rights.Contains("log.view") &&
                                    !sessionInfo.Rights.Contains("admin.all")))
                    {
                        req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                        return;
                    }
                    else
                    {
                        sessionInfo.LastSeenTickCount = Environment.TickCount;
                    }

                    HandleWebSocketLog(req, m_LogHtmlReceivers);
                }
                else if (req.RawUrl.StartsWith("/admin/console/"))
                {
                    if (!req.IsWebSocket)
                    {
                        req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                        return;
                    }

                    SessionInfo sessionInfo;
                    string sessionKey = req.CallerIP + "+" + req.RawUrl.Substring(15);
                    if (!m_Sessions.TryGetValue(sessionKey, out sessionInfo) ||
                                    !sessionInfo.IsAuthenticated ||
                                    (!sessionInfo.Rights.Contains("console.access") &&
                                    !sessionInfo.Rights.Contains("admin.all")))
                    {
                        req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                        return;
                    }
                    else
                    {
                        sessionInfo.LastSeenTickCount = Environment.TickCount;
                    }

                    HandleWebSocketConsole(req, req.RawUrl.Substring(15));
                }
                else if (req.RawUrl.StartsWith("/admin/json"))
                {
                    if (req.Method != "POST")
                    {
#if DEBUG
                        m_Log.DebugFormat("Method {0} not allowed to /admin/json", req.Method);
#endif
                        req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    }
                    else if (req.ContentType != JsonContentType)
                    {
#if DEBUG
                        m_Log.DebugFormat("Content-Type '{0}' not allowed to /admin/json", req.ContentType);
#endif
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
                        catch (Json.InvalidJsonSerializationException)
                        {
                            req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                            return;
                        }
                        if (jsondata == null)
                        {
                            ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                            return;
                        }
                        Action<HttpRequest, Map> del;
                        if (!jsondata.ContainsKey("method"))
                        {
                            ErrorResponse(req, AdminWebIfErrorResult.MissingMethod);
                            return;
                        }

                        string methodName = jsondata["method"].ToString();
                        string sessionKey;

#if DEBUG
                        m_Log.DebugFormat("/admin/json Method called {0}", methodName);
#endif

                        switch (methodName)
                        {
                            case "login":
                                HandleLoginRequest(req, jsondata);
                                break;

                            case "challenge":
                                HandleChallengeRequest(req, jsondata);
                                break;

                            default:
                                if (!jsondata.ContainsKey("sessionid"))
                                {
                                    ErrorResponse(req, AdminWebIfErrorResult.MissingSessionId);
                                    return;
                                }
                                sessionKey = req.CallerIP + "+" + jsondata["sessionid"].ToString();
                                if (!m_Sessions.TryGetValue(sessionKey, out sessionInfo) ||
                                    !sessionInfo.IsAuthenticated)
                                {
                                    ErrorResponse(req, AdminWebIfErrorResult.NotLoggedIn);
                                    return;
                                }
                                else
                                {
                                    sessionInfo.LastSeenTickCount = Environment.TickCount;
                                }
                                if (methodName == "logout")
                                {
                                    m_Sessions.Remove(sessionKey);
                                    SuccessResponse(req, new Map());
                                }
                                else if (!JsonMethods.TryGetValue(methodName, out del))
                                {
                                    ErrorResponse(req, AdminWebIfErrorResult.UnknownMethod);
                                    return;
                                }
                                else
                                {
                                    if (!sessionInfo.Rights.Contains("admin.all"))
                                    {
                                        bool isRightRequired = false;
                                        bool hasRightRequired = false;
                                        AdminWebIfRequiredRightAttribute[] attrs = Attribute.GetCustomAttributes(del.GetType(), typeof(AdminWebIfRequiredRightAttribute[])) as AdminWebIfRequiredRightAttribute[];
                                        foreach (AdminWebIfRequiredRightAttribute attr in attrs)
                                        {
                                            isRightRequired = true;
                                            if (sessionInfo.Rights.Contains(attr.Right))
                                            {
                                                hasRightRequired = true;
                                                break;
                                            }
                                        }
                                        if (isRightRequired && !hasRightRequired)
                                        {
                                            ErrorResponse(req, AdminWebIfErrorResult.InsufficientRights);
                                            return;
                                        }
                                    }
                                    del(req, jsondata);
                                }
                                break;
                        }
                    }
                }
                else if (req.RawUrl.StartsWith("/admin/js/") || req.RawUrl.StartsWith("/admin/css/"))
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
            catch(HttpResponse.ConnectionCloseException)
            {
                throw;
            }
            catch(WebSocketClosedException)
            {
                /* ignore this one as it results from WebSocket close */
            }
            catch(Exception e)
            {
                m_Log.ErrorFormat("Exception encountered! {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
            }
        }
        #endregion

        #region HTTP File Serving
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
                    else if (filepath.EndsWith(".css"))
                    {
                        contentType = "text/css";
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
            catch(DirectoryNotFoundException)
            {
                ServeFileNotFoundResponse(req);
            }
            catch (FileNotFoundException)
            {
                ServeFileNotFoundResponse(req);
            }
        }

        void ServeFileNotFoundResponse(HttpRequest req)
        {
            using (HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, "Not Found"))
            {
                res.ContentType = "text/html";
                using (Stream o = res.GetOutputStream())
                {
                    using (StreamWriter w = o.UTF8StreamWriter())
                    {
                        w.Write("<html><head><title>Not Found</title><body>Not Found</body></html>");
                    }
                }
            }
        }
        #endregion

        #region Commands
        void DisplayAdminWebIFHelp(TTY io)
        {
            string chgpwcmd = string.Empty;
            if(m_EnableSetPasswordCommand)
            {
                chgpwcmd = "admin-webif change password <user>\nadmin-webif change password <user> <pass>\n";
            }
            io.Write("admin-webif show users\n" +
                "admin-webif show user <user>\n" +
                chgpwcmd +
                "admin-webif delete user <user>\n" +
                "admin-webif grant <user> <right>\n" +
                "admin-webif revoke <user> <right>\n" +
                "admin-webif show json-methods\n" +
                "admin-webif show modules");
        }

        public void AdminWebIFCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            if(limitedToScene != UUID.Zero)
            {
                io.Write("admin-webif command is not allowed on restricted console");
                return;
            }
            else if(args[0] == "help" || args.Count < 2)
            {
                DisplayAdminWebIFHelp(io);
            }
            else
            {
                switch(args[1])
                {
                    case "show":
                        if(args.Count < 3)
                        {
                            DisplayAdminWebIFHelp(io);
                        }
                        else
                        {
                            switch(args[2])
                            {
                                case "users":
                                    {
                                        StringBuilder output = new StringBuilder("User List: --------------------");
                                        foreach (string name in m_ServerParams[UUID.Zero])
                                        {
                                            if (name.StartsWith("WebIF.Admin.User.") &&
                                                name.EndsWith(".PassCode"))
                                            {
                                                string username = name.Substring(17, name.Length - 17 - 9);
                                                output.Append("\n");
                                                output.Append(username);
                                            }
                                        }
                                        io.Write(output.ToString());
                                    }
                                    break;

                                case "user":
                                    if(args.Count < 4)
                                    {
                                        DisplayAdminWebIFHelp(io);
                                    }
                                    else
                                    {
                                        string userRef = "WebIF.Admin.User." + args[3];
                                        string rights;
                                        if(m_ServerParams.TryGetValue(UUID.Zero, userRef + ".Rights", out rights))
                                        {
                                            StringBuilder output = new StringBuilder("Rights: --------------------");
                                            foreach(string right in rights.Split(','))
                                            {
                                                output.Append("\n");
                                                output.Append(right.Trim());
                                            }
                                            io.Write(output.ToString());
                                        }
                                        else
                                        {
                                            io.WriteFormatted("User '{0}' does not exist", args[3]);
                                        }
                                    }
                                    break;

                                case "json-methods":
                                    {
                                        StringBuilder output = new StringBuilder("Json Methods: --------------------");
                                        foreach (KeyValuePair<string, Action<HttpRequest, Map>> kvp in m_JsonMethods)
                                        {
                                            output.Append("\n");
                                            output.Append(kvp.Key);
                                        }
                                        io.Write(output.ToString());
                                    }
                                    break;

                                case "modules":
                                    {
                                        StringBuilder output = new StringBuilder("Modules: --------------------");
                                        foreach (string id in m_Modules)
                                        {
                                            output.Append("\n");
                                            output.Append(id);
                                        }
                                        io.Write(output.ToString());
                                    }
                                    break;

                                default:
                                    DisplayAdminWebIFHelp(io);
                                    break;
                            }
                        }
                        break;

                    case "change":
                        if(args.Count < 3)
                        {
                            DisplayAdminWebIFHelp(io);
                        }
                        else
                        {
                            switch(args[2])
                            {
                                case "password":
                                    if(args.Count < 4 || !m_EnableSetPasswordCommand)
                                    {
                                        DisplayAdminWebIFHelp(io);
                                    }
                                    else if(args.Count == 4)
                                    {
                                        io.Write(SetUserPassword(args[3], io.GetPass("Password")) ?
                                            "Password changed." :
                                            "User does not exist.");
                                    }
                                    else
                                    {
                                        io.Write(SetUserPassword(args[3], args[4]) ?
                                            "Password changed." :
                                            "User does not exist.");
                                    }
                                    break;

                                default:
                                    DisplayAdminWebIFHelp(io);
                                    break;
                            }
                        }
                        break;

                    case "delete":
                        if(args.Count < 3)
                        {
                            DisplayAdminWebIFHelp(io);
                        }
                        else
                        {
                            switch(args[2])
                            {
                                case "user":
                                    if(args.Count < 4)
                                    {
                                        DisplayAdminWebIFHelp(io);
                                    }
                                    else
                                    {
                                        string userRef = "WebIF.Admin.User." + args[3];
                                        m_ServerParams.Remove(UUID.Zero, userRef + ".PassCode");
                                        m_ServerParams.Remove(UUID.Zero, userRef + ".Rights");
                                    }
                                    break;

                                default:
                                    DisplayAdminWebIFHelp(io);
                                    break;
                            }
                        }
                        break;

                    case "grant":
                        if(args.Count < 4)
                        {
                            DisplayAdminWebIFHelp(io);
                        }
                        else
                        {
                            string s;
                            string paraname = "WebIF.Admin.User." + args[2] + "Rights";
                            if(!m_ServerParams.TryGetValue(UUID.Zero, paraname, out s))
                            {
                                io.Write("User not found.");
                            }
                            else
                            {
                                List<string> rights = new List<string>();
                                foreach(string i in s.Split(','))
                                {
                                    rights.Add(i.Trim());
                                }
                                if (!rights.Contains(args[3]))
                                {
                                    rights.Add(args[3]);
                                }
                                m_ServerParams[UUID.Zero, paraname] = string.Join(",", rights);
                            }
                        }
                        break;

                    case "revoke":
                        if (args.Count < 4)
                        {
                            DisplayAdminWebIFHelp(io);
                        }
                        else
                        {
                            string s;
                            string paraname = "WebIF.Admin.User." + args[2] + "Rights";
                            if (!m_ServerParams.TryGetValue(UUID.Zero, paraname, out s))
                            {
                                io.Write("User not found.");
                            }
                            else
                            {
                                List<string> rights = new List<string>();
                                foreach (string i in s.Split(','))
                                {
                                    rights.Add(i.Trim());
                                }
                                rights.Remove(args[3]);
                                m_ServerParams[UUID.Zero, paraname] = string.Join(",", rights);
                            }
                        }
                        break;

                    default:
                        io.Write("Unknown admin-webif operation " + args[1]);
                        break;
                }
            }
        }
        #endregion

        #region WebIF admin functions
        void AvailableModulesList(HttpRequest req, Map jsondata)
        {
            AnArray res = new AnArray();
            foreach(string module in m_Modules)
            {
                res.Add(module);
            }
            Map m = new Map();
            m.Add("title", m_Title);
            m.Add("modules", res);
            SuccessResponse(req, m);
        }

        [AdminWebIfRequiredRight("dnscache.manage")]
        void DnsCacheList(HttpRequest req, Map jsondata)
        {
            AnArray res = new AnArray();
            foreach (string host in DnsNameCache.GetCachedDnsEntries())
            {
                res.Add(host);
            }
            Map m = new Map();
            m.Add("entries", res);
            SuccessResponse(req, m);
        }

        [AdminWebIfRequiredRight("dnscache.manage")]
        void DnsCacheRemove(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("host"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            if(DnsNameCache.RemoveCachedDnsEntry(jsondata["host"].ToString()))
            {
                SuccessResponse(req, new Map());
            }
            else
            {
                ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
        }

        [AdminWebIfRequiredRight("modules.view")]
        void ModulesList(HttpRequest req, Map jsondata)
        {
            AnArray res = new AnArray();
            Dictionary<string, IPlugin> plugins = m_Loader.AllServices;
            foreach(KeyValuePair<string, IPlugin> kvp in plugins)
            {
                Map pluginData = new Map();
                pluginData.Add("Name", kvp.Key);
                DescriptionAttribute descAttr = Attribute.GetCustomAttribute(kvp.Value.GetType(), typeof(DescriptionAttribute)) as DescriptionAttribute;
                pluginData.Add("Description", descAttr != null ? descAttr.Description : string.Empty);
                res.Add(pluginData);
            }
            Map m = new Map();
            m.Add("modules", res);
            SuccessResponse(req, m);
        }

        [AdminWebIfRequiredRight("modules.view")]
        void ModuleGet(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("name"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            Dictionary<string, IPlugin> plugins = m_Loader.AllServices;
            IPlugin plugin;
            if(plugins.TryGetValue(jsondata["name"].ToString(), out plugin))
            {
                Map res = new Map();
                res.Add("Name", jsondata["name"].ToString());
                Type pluginType = plugin.GetType();
                DescriptionAttribute descAttr = Attribute.GetCustomAttribute(pluginType, typeof(DescriptionAttribute)) as DescriptionAttribute;
                res.Add("Description", descAttr != null ? descAttr.Description : string.Empty);

                AnArray featuresList = new AnArray();

                foreach (KeyValuePair<Type, string> kvp in ConfigurationLoader.FeaturesTable)
                {
                    if (kvp.Key.IsInterface)
                    {
                        if (pluginType.GetInterfaces().Contains(kvp.Key))
                        {
                            featuresList.Add(kvp.Value);
                        }
                    }
                    else if (kvp.Key.IsAssignableFrom(pluginType))
                    {
                        featuresList.Add(kvp.Value);
                    }
                }

                res.Add("Features", featuresList);
                SuccessResponse(req, res);
            }
            else
            {
                ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
        }

        [AdminWebIfRequiredRight("issues.view")]
        void IssuesView(HttpRequest req, Map jsondata)
        {
            AnArray res = new AnArray();
            foreach(string s in m_KnownConfigurationIssues)
            {
                res.Add(s);
            }
            Map mres = new Map();
            mres["issues"] = res;
            SuccessResponse(req, mres);
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        void ShowServerParams(HttpRequest req, Map jsondata)
        {
            AnArray res = new AnArray();
            Dictionary<string, ServerParamAttribute> resList = new Dictionary<string, ServerParamAttribute>();
            foreach (KeyValuePair<string, ServerParamAttribute> kvp in m_Loader.ServerParams)
            {
                ServerParamAttribute paraType;
                if (!resList.TryGetValue(kvp.Key, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                {
                    resList[kvp.Key] = kvp.Value;
                }
            }

            foreach (KeyValuePair<string, ServerParamAttribute> kvp in resList)
            {
                Map eres = new Map();
                eres.Add("name", kvp.Key);
                if (!string.IsNullOrEmpty(kvp.Value.Description))
                {
                    eres.Add("description", kvp.Value.Description);
                }
                eres.Add("type", kvp.Value.Type.ToString());
                Type paraType = kvp.Value.ParameterType;
                if(paraType == typeof(bool))
                {
                    eres.Add("valuerange", "bool");
                }
                else if(paraType == typeof(uint))
                {
                    eres.Add("valuerange", "uint");
                }
                else if (paraType == typeof(int))
                {
                    eres.Add("valuerange", "int");
                }
                else
                {
                    eres.Add("valuerange", "string");
                }
            }
            Map mres = new Map();
            mres["serverparams"] = res;
            SuccessResponse(req, mres);
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        void SetServerParam(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("parameter") || !jsondata.ContainsKey("value"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                UUID regionid = UUID.Zero;
                if(jsondata.ContainsKey("regionid") && !UUID.TryParse(jsondata["regionid"].ToString(), out regionid))
                {
                    ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }

                string parameter = jsondata["parameter"].ToString();
                string value = jsondata["value"].ToString();
                if(parameter.StartsWith("WebIF.Admin.User."))
                {
                    ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }

                try
                {
                    m_ServerParams[regionid, parameter] = value;
                }
                catch
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                    return;
                }
                SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        void GetServerParams(HttpRequest req, Map jsondata)
        {
            IValue ipara;
            AnArray paradata;
            if(!jsondata.TryGetValue("parameters", out ipara) ||
                null == (paradata = ipara as AnArray))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                AnArray resultlist = new AnArray();
                foreach(IValue iv in paradata)
                {
                    Map reqdata = iv as Map;
                    if(null == reqdata || !reqdata.ContainsKey("parameter"))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    UUID regionid = UUID.Zero;
                    if (jsondata.ContainsKey("regionid") && !UUID.TryParse(jsondata["regionid"].ToString(), out regionid))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    string parameter = jsondata["parameter"].ToString();
                    string value;
                    if (parameter.StartsWith("WebIF.Admin.User."))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    try
                    {
                        value = m_ServerParams[regionid, parameter];
                    }
                    catch
                    {
                        /* no data */
                    }
                }
                Map res = new Map();
                res.Add("values", resultlist);
                SuccessResponse(req, res);
            }
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        void GetServerParam(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("parameter"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                UUID regionid = UUID.Zero;
                if (jsondata.ContainsKey("regionid") && !UUID.TryParse(jsondata["regionid"].ToString(), out regionid))
                {
                    ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }

                string parameter = jsondata["parameter"].ToString();
                string value;
                if (parameter.StartsWith("WebIF.Admin.User."))
                {
                    ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }

                try
                {
                    value = m_ServerParams[regionid, parameter];
                }
                catch
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                    return;
                }
                Map res = new Map();
                res.Add("value", value);
                SuccessResponse(req, res);
            }
        }

        [AdminWebIfRequiredRight("webif.admin.users.manage")]
        void GrantRight(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user") || !jsondata.ContainsKey("right"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                string userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";
                string pass_sha1;
                string rights;

                if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                    m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
                {
                    string[] rightlist = rights.ToLower().Split(',');
                    List<string> rightlistnew = new List<string>();
                    AnArray resdata = new AnArray();
                    foreach(string r in rightlist)
                    {
                        rightlistnew.Add(r.Trim());
                        resdata.Add(r.Trim());
                    }
                    if(!rightlistnew.Contains(jsondata["right"].ToString().ToLower()))
                    {
                        rightlistnew.Add(jsondata["right"].ToString().ToLower());
                        resdata.Add(jsondata["right"].ToString().ToLower());
                    }
                    m_ServerParams[UUID.Zero, userRef + "Rights"] = string.Join(",", rightlistnew);
                    Map m = new Map();
                    m["user"] = jsondata["user"];
                    m["rights"] = resdata;
                    SuccessResponse(req, m);
                }
                else
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                }
            }
        }

        [AdminWebIfRequiredRight("webif.admin.users.manage")]
        void RevokeRight(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user") || !jsondata.ContainsKey("right"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                string userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";
                string pass_sha1;
                string rights;

                if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                    m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
                {
                    string[] rightlist = rights.ToLower().Split(',');
                    List<string> rightlistnew = new List<string>();
                    AnArray resdata = new AnArray();
                    foreach (string r in rightlist)
                    {
                        string trimmed = r.Trim();
                        if (trimmed != jsondata["right"].ToString())
                        {
                            rightlistnew.Add(r.Trim());
                            resdata.Add(r.Trim());
                        }
                    }
                    m_ServerParams[UUID.Zero, userRef + "Rights"] = string.Join(",", rightlistnew);
                    Map m = new Map();
                    m["user"] = jsondata["user"];
                    m["rights"] = resdata;
                    SuccessResponse(req, m);
                }
                else
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                }
            }
        }

        [AdminWebIfRequiredRight("webif.admin.users.manage")]
        void DeleteUser(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                string userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";

                m_ServerParams.Remove(UUID.Zero, userRef + "Rights");
                if (m_ServerParams.Remove(UUID.Zero, userRef + "PassCode"))
                {
                    SuccessResponse(req, new Map());
                }
                else
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                }
            }
        }
        #endregion
    }
    #endregion

    #region Factory
    [PluginName("AdminWebIF")]
    public class AdminWebIFFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ADMIN WEB IF");

        public AdminWebIFFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            bool enableSetPasswordCommand = ownSection.GetBoolean("EnableSetPasswordCommand",
#if DEBUG
                    true
#else
                    false
#endif
                    );

            if (enableSetPasswordCommand)
            {
                loader.KnownConfigurationIssues.Add("Set EnableSetPasswordCommand=false in section [" + ownSection.Name + "]");
                m_Log.ErrorFormat("[SECURITY] Set EnableSetPasswordCommand=false in section [{0}]", ownSection.Name);
            }
            return new AdminWebIF(
                ownSection.GetString("BasePath", ""),
                enableSetPasswordCommand,
                ownSection.GetString("AvatarNameServices", "AvatarNameStorage").Trim(),
                ownSection.GetString("Title", "SilverSim"));
        }
    }
#endregion
}
