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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using log4net;
using log4net.Core;
using Nini.Config;
using SilverSim.Http;
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
    [Description("Administration Web-Interface (WebIF)")]
    [PluginName("AdminWebIF")]
    public sealed class AdminWebIF : IPlugin, IPluginShutdown, IPostLoadStep, IAdminWebIF
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ADMIN WEB IF");

        private ServerParamServiceInterface m_ServerParams;
        private BaseHttpServer m_HttpServer;
        private BaseHttpServer m_HttpsServer;
        private readonly string m_BasePath;
        private const string JsonContentType = "application/json";
        private RwLockedList<string> m_KnownConfigurationIssues;
        private ConfigurationLoader m_Loader;
        private AggregatingAvatarNameService m_AvatarNameService;
        private readonly string m_AvatarNameServiceNames;
        private readonly string m_Title;
        private readonly BlockingQueue<LoggingEvent> m_LogEventQueue = new BlockingQueue<LoggingEvent>();

        private class SessionInfo
        {
            public int LastSeenTickCount = Environment.TickCount;
            public bool IsAuthenticated;
            public string UserName = string.Empty;
            public string ExpectedResponse = string.Empty;
            public List<string> Rights = new List<string>();
            public UUID SelectedSceneID = UUID.Zero;
        }

        private readonly RwLockedDictionary<string, SessionInfo> m_Sessions = new RwLockedDictionary<string, SessionInfo>();
        public readonly RwLockedDictionaryAutoAdd<string, RwLockedList<string>> m_AutoGrantRights = new RwLockedDictionaryAutoAdd<string, RwLockedList<string>>(() => new RwLockedList<string>());
        public readonly RwLockedDictionary<string, Action<HttpRequest, Map>> m_JsonMethods = new RwLockedDictionary<string, Action<HttpRequest, Map>>();
        public readonly RwLockedList<string> m_Modules = new RwLockedList<string>();
        public readonly RwLockedList<HttpWebSocket> m_LogPlainReceivers = new RwLockedList<HttpWebSocket>();
        public readonly RwLockedList<HttpWebSocket> m_LogHtmlReceivers = new RwLockedList<HttpWebSocket>();

        public RwLockedDictionaryAutoAdd<string, RwLockedList<string>> AutoGrantRights => m_AutoGrantRights;

        public RwLockedDictionary<string, Action<HttpRequest, Map>> JsonMethods => m_JsonMethods;

        public RwLockedList<string> ModuleNames => m_Modules;

        private readonly Timer m_Timer = new Timer(1);
        private readonly bool m_EnableSetPasswordCommand;

        private bool m_ShutdownHandlerThreads;

        private static readonly string[] LogColors =
        {
            "blue",
            "green",
            "cyan",
            "magenta",
            "yellow"
        };

        private void LogThread()
        {
            System.Threading.Thread.CurrentThread.Name = "WebIF LogThread";
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

                var plaintext_conns = new List<HttpWebSocket>();
                foreach (HttpWebSocket sock in m_LogPlainReceivers)
                {
                    plaintext_conns.Add(sock);
                }

                var html_conns = new List<HttpWebSocket>();
                foreach (HttpWebSocket sock in m_LogHtmlReceivers)
                {
                    html_conns.Add(sock);
                }

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
                    colorbegin = "<span style=\"color: " + LogColors[Math.Abs(logevent.LoggerName.ToUpper().GetHashCode()) % LogColors.Length] + "\">";
                    colorend = "</span>";
                }
                string msg = string.Format("{0}{1} - [{2}{3}{4}]: {5}{6}",
                    fullcolorbegin,
                    System.Web.HttpUtility.HtmlEncode(logevent.TimeStamp.ToString()),
                    colorbegin,
                    System.Web.HttpUtility.HtmlEncode(logevent.LoggerName),
                    colorend,
                    System.Web.HttpUtility.HtmlEncode(logevent.RenderedMessage),
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
                    logevent.RenderedMessage);
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
            var m = new Map
            {
                { "success", false },
                { "reason", (int)reason }
            };
            using (HttpResponse res = req.BeginResponse(JsonContentType))
            {
                using (Stream o = res.GetOutputStream())
                {
                    Json.Serialize(m, o);
                }
            }
        }
        #endregion

        public AdminWebIF(ConfigurationLoader loader, IConfig ownSection)
        {
            m_EnableSetPasswordCommand = ownSection.GetBoolean("EnableSetPasswordCommand",
#if DEBUG
                    true
#else
                    false
#endif
                    );

            if (m_EnableSetPasswordCommand)
            {
                loader.KnownConfigurationIssues.Add("Set EnableSetPasswordCommand=false in section [" + ownSection.Name + "]");
                m_Log.ErrorFormat("[SECURITY] Set EnableSetPasswordCommand=false in section [{0}]", ownSection.Name);
            }
            m_BasePath = ownSection.GetString("BasePath", string.Empty);
            m_AvatarNameServiceNames = ownSection.GetString("AvatarNameServices", "AvatarNameStorage").Trim();
            m_Title = ownSection.GetString("Title", "SilverSim");

            m_Timer.Elapsed += HandleTimer;
            JsonMethods.Add("webif.admin.user.grantright", GrantRight);
            JsonMethods.Add("webif.admin.user.revokeright", RevokeRight);
            JsonMethods.Add("webif.admin.user.delete", DeleteUser);
            JsonMethods.Add("session.validate", HandleSessionValidateRequest);
            JsonMethods.Add("serverparam.get", GetServerParam);
            JsonMethods.Add("serverparams.get", GetServerParams);
            JsonMethods.Add("serverparams.get.explicitly", GetServerParamsExplicitly);
            JsonMethods.Add("serverparams.show", ShowServerParams);
            JsonMethods.Add("serverparam.set", SetServerParam);
            JsonMethods.Add("issues.get", IssuesView);
            JsonMethods.Add("modules.list", ModulesList);
            JsonMethods.Add("module.get", ModuleGet);
            JsonMethods.Add("dnscache.list", DnsCacheList);
            JsonMethods.Add("dnscache.delete", DnsCacheRemove);
            JsonMethods.Add("webif.modules", AvailableModulesList);
            JsonMethods.Add("avatarname.search.exact", HandleFindExactUser);
            JsonMethods.Add("avatarname.search", HandleFindUser);
            JsonMethods.Add("avatarname.getdetails", HandleGetUserDetails);
            LogController.Queues.Add(m_LogEventQueue);
            ThreadManager.CreateThread(LogThread).Start();
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

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

        private void HandleTimer(object o, EventArgs args)
        {
            var removeList = new List<string>();
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

        private const string AdminUserReference = "WebIF.Admin.User.admin.";

        #region Initialization
        public void Startup(ConfigurationLoader loader)
        {
            m_Loader = loader;
            var avatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
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
            if(loader.TryGetHttpsServer(out m_HttpsServer))
            {
                m_HttpsServer.StartsWithUriHandlers.Add("/admin", HandleHttp);
            }
            else
            {
                m_HttpsServer = null;
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
                using (var sha1 = SHA1.Create())
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
            if (m_HttpsServer != null)
            {
                m_HttpsServer.StartsWithUriHandlers.Remove("/admin");
            }
            m_Timer.Elapsed -= HandleTimer;
            m_Timer.Dispose();
        }
        #endregion

        #region User Logic
        private bool SetUserPassword(string user, string pass)
        {
            string userRef = "WebIF.Admin.User." + user + ".PassCode";
            string oldPass;
            if (m_ServerParams.TryGetValue(UUID.Zero, userRef, out oldPass))
            {
                string res;
                using (var sha1 = SHA1.Create())
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

        private void FindUser(SessionInfo sessionInfo, UUID challenge)
        {
            string userRef = "WebIF.Admin.User." + sessionInfo.UserName + ".";
            string pass_sha1;
            string rights;

            if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
            {
                using (var sha1 = SHA1.Create())
                {
                    var str = (challenge.ToString().ToLower() + "+" + pass_sha1.ToLower()).ToUTF8Bytes();

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
            if(m_HttpsServer == null || m_ServerParams.GetBoolean(UUID.Zero, "WebIF.Admin.EnableHTTP", m_HttpsServer == null))
            {
                HandleHttp(req);
            }
            else
            {
                string host;
                var relocation_uri = m_HttpsServer.ServerURI + "admin";
                Uri uri;
                if (req.TryGetHeader("Host", out host) &&
                    Uri.TryCreate("http://" + host, UriKind.Absolute, out uri))
                {
                    relocation_uri = "https://" + uri.Host + ":" + m_HttpsServer.Port.ToString() + "/admin";
                }

                using (HttpResponse res = req.BeginResponse(HttpStatusCode.MovedPermanently, "Moved Permanently"))
                {
                    res.Headers.Add("Location", relocation_uri);
                }
            }
        }

        private void HandleSessionValidateRequest(HttpRequest req, Map jsonreq)
        {
            SuccessResponse(req, new Map());
        }

        private void HandleLoginRequest(HttpRequest req, Map jsonreq)
        {
            if(!jsonreq.ContainsKey("sessionid") || !jsonreq.ContainsKey("user") || !jsonreq.ContainsKey("response"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            var sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
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
                    var res = new Map();
                    var rights = new AnArray();
                    foreach(var right in sessionInfo.Rights)
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
                    using (var httpres = req.BeginResponse(JsonContentType))
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
            var sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
            SessionInfo info;
            if(m_Sessions.TryGetValue(sessionKey,out info))
            {
                return info.SelectedSceneID;
            }
            return UUID.Zero;
        }

        public void SetSelectedRegion(HttpRequest req, Map jsonreq, UUID sceneID)
        {
            var sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
            SessionInfo info;
            if (m_Sessions.TryGetValue(sessionKey, out info))
            {
                info.SelectedSceneID = sceneID;
            }
        }

        private void HandleChallengeRequest(HttpRequest req, Map jsonreq)
        {
            var sessionID = UUID.Random;
            var challenge = UUID.Random;
            var resdata = new Map
            {
                ["sessionid"] = sessionID,
                ["challenge"] = challenge
            };
            if (!jsonreq.ContainsKey("user"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            var sessionInfo = new SessionInfo();
            m_Sessions.Add(req.CallerIP + "+" + sessionID.ToString(), sessionInfo);
            sessionInfo.UserName = jsonreq["user"].ToString().ToLower();
            FindUser(sessionInfo, challenge);
            SuccessResponse(req, resdata);
        }

        private sealed class WebSocketTTY : TTY, IDisposable
        {
            private readonly HttpWebSocket m_Socket;
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

            private void DisableEcho()
            {
                var echoOff = "disable echo".ToUTF8Bytes();
                m_Socket.WriteBinary(echoOff, 0, echoOff.Length);
            }

            private void EnableEcho()
            {
                var echoOn = "enable echo".ToUTF8Bytes();
                m_Socket.WriteBinary(echoOn, 0, echoOn.Length);
            }

            public override string ReadLine(string p, bool echoInput)
            {
                var text = new StringBuilder();
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

        private void HandleWebSocketConsole(HttpRequest req, string sessionid)
        {
            var jsondata = new Map
            {
                { "sessionid", sessionid }
            };
            using (var tty = new WebSocketTTY(req.BeginWebSocket("console")))
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

        private void HandleWebSocketLog(HttpRequest req, RwLockedList<HttpWebSocket> logreceivers)
        {
            using (var sock = req.BeginWebSocket("log"))
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
                    /* intentionally ignored */
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
                    var sessionKey = req.CallerIP + "+" + req.RawUrl.Substring(11);
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
                    var sessionKey = req.CallerIP + "+" + req.RawUrl.Substring(15);
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
                    var sessionKey = req.CallerIP + "+" + req.RawUrl.Substring(15);
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

                        var methodName = jsondata["method"].ToString();
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
                                        foreach (var attr in Attribute.GetCustomAttributes(del.GetType(), typeof(AdminWebIfRequiredRightAttribute[])) as AdminWebIfRequiredRightAttribute[])
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
        private void ServeFile(HttpRequest req, string filepath)
        {
            try
            {
                using (var file = new FileStream("../data/adminpages/" + filepath, FileMode.Open))
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

                    using (var res = req.BeginResponse(contentType))
                    {
                        using (var o = res.GetOutputStream())
                        {
                            file.CopyTo(o);
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

        private void ServeFileNotFoundResponse(HttpRequest req)
        {
            using (HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, "Not Found"))
            {
                res.ContentType = "text/html";
                using (var o = res.GetOutputStream())
                {
                    using (var w = o.UTF8StreamWriter())
                    {
                        w.Write("<html><head><title>Not Found</title><body>Not Found</body></html>");
                    }
                }
            }
        }
        #endregion

        #region Commands
        private void DisplayAdminWebIFHelp(TTY io)
        {
            var chgpwcmd = string.Empty;
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
                                        var output = new StringBuilder("User List: --------------------");
                                        foreach (var name in m_ServerParams[UUID.Zero])
                                        {
                                            if (name.StartsWith("WebIF.Admin.User.") &&
                                                name.EndsWith(".PassCode"))
                                            {
                                                var username = name.Substring(17, name.Length - 17 - 9);
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
                                        var userRef = "WebIF.Admin.User." + args[3];
                                        string rights;
                                        if(m_ServerParams.TryGetValue(UUID.Zero, userRef + ".Rights", out rights))
                                        {
                                            var output = new StringBuilder("Rights: --------------------");
                                            foreach(var right in rights.Split(','))
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
                                        var output = new StringBuilder("Json Methods: --------------------");
                                        foreach (var kvp in m_JsonMethods)
                                        {
                                            output.Append("\n");
                                            output.Append(kvp.Key);
                                        }
                                        io.Write(output.ToString());
                                    }
                                    break;

                                case "modules":
                                    {
                                        var output = new StringBuilder("Modules: --------------------");
                                        foreach (var id in m_Modules)
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
                                        var userRef = "WebIF.Admin.User." + args[3];
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
                            var paraname = "WebIF.Admin.User." + args[2] + "Rights";
                            if(!m_ServerParams.TryGetValue(UUID.Zero, paraname, out s))
                            {
                                io.Write("User not found.");
                            }
                            else
                            {
                                var rights = new List<string>();
                                foreach(var i in s.Split(','))
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
                            var paraname = "WebIF.Admin.User." + args[2] + "Rights";
                            if (!m_ServerParams.TryGetValue(UUID.Zero, paraname, out s))
                            {
                                io.Write("User not found.");
                            }
                            else
                            {
                                var rights = new List<string>();
                                foreach (var i in s.Split(','))
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
        private void HandleFindExactUser(HttpRequest req, Map jsondata)
        {
            IValue q1;
            IValue q2;

            if (!jsondata.TryGetValue("firstname", out q1) || !jsondata.TryGetValue("lastname", out q2))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            UUI uui;
            if(!m_AvatarNameService.TryGetValue(q1.ToString(), q2.ToString(), out uui))
            {
                ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            if(uui.HomeURI == null)
            {
                uui.HomeURI = new Uri(m_Loader.HomeURI);
            }

            var resdata = new Map
            {
                ["user"] = uui.ToMap()
            };
            SuccessResponse(req, resdata);
        }

        private void HandleFindUser(HttpRequest req, Map jsondata)
        {
            IValue q1;
            IValue q2;
            List<UUI> uuis;

            if(jsondata.TryGetValue("firstname", out q1) && jsondata.TryGetValue("lastname", out q2))
            {
                uuis = m_AvatarNameService.Search(new string[] { q1.ToString(), q2.ToString() });
            }
            else if(jsondata.TryGetValue("query", out q1))
            {
                uuis = m_AvatarNameService.Search(new string[] { q1.ToString() });
            }
            else
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            var resdata = new Map();
            var resarray = new AnArray();
            foreach(var uui in uuis)
            {
                if (uui.HomeURI == null)
                {
                    uui.HomeURI = new Uri(m_Loader.HomeURI);
                }
                resarray.Add(uui.ToMap());
            }
            resdata.Add("uuis", resarray);
            SuccessResponse(req, resdata);
        }

        private void HandleGetUserDetails(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("uuid"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            UUI uui;
            if(!m_AvatarNameService.TryGetValue(jsondata["uuid"].AsUUID, out uui))
            {
                ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                if (uui.HomeURI == null)
                {
                    uui.HomeURI = new Uri(m_Loader.HomeURI);
                }
                var res = new Map
                {
                    ["user"] = uui.ToMap()
                };
                SuccessResponse(req, res);
            }
        }

        private void AvailableModulesList(HttpRequest req, Map jsondata)
        {
            var res = new AnArray();
            foreach(var module in m_Modules)
            {
                res.Add(module);
            }
            var m = new Map
            {
                { "title", m_Title },
                { "modules", res }
            };
            SuccessResponse(req, m);
        }

        [AdminWebIfRequiredRight("dnscache.manage")]
        private void DnsCacheList(HttpRequest req, Map jsondata)
        {
            var res = new AnArray();
            foreach (var host in DnsNameCache.GetCachedDnsEntries())
            {
                res.Add(host);
            }
            var m = new Map
            {
                ["entries"] = res
            };
            SuccessResponse(req, m);
        }

        [AdminWebIfRequiredRight("dnscache.manage")]
        private void DnsCacheRemove(HttpRequest req, Map jsondata)
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
        private void ModulesList(HttpRequest req, Map jsondata)
        {
            var res = new AnArray();
            foreach(var kvp in m_Loader.AllServices)
            {
                var pluginData = new Map
                {
                    { "Name", kvp.Key }
                };
                var descAttr = Attribute.GetCustomAttribute(kvp.Value.GetType(), typeof(DescriptionAttribute)) as DescriptionAttribute;
                pluginData.Add("Description", descAttr != null ? descAttr.Description : string.Empty);
                res.Add(pluginData);
            }
            var m = new Map
            {
                ["modules"] = res
            };
            SuccessResponse(req, m);
        }

        [AdminWebIfRequiredRight("modules.view")]
        private void ModuleGet(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("name"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            var plugins = m_Loader.AllServices;
            IPlugin plugin;
            if(plugins.TryGetValue(jsondata["name"].ToString(), out plugin))
            {
                var res = new Map
                {
                    { "Name", jsondata["name"].ToString() }
                };
                var pluginType = plugin.GetType();
                var descAttr = Attribute.GetCustomAttribute(pluginType, typeof(DescriptionAttribute)) as DescriptionAttribute;
                res.Add("Description", descAttr != null ? descAttr.Description : string.Empty);

                var featuresList = new AnArray();

                foreach (var kvp in ConfigurationLoader.FeaturesTable)
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
        private void IssuesView(HttpRequest req, Map jsondata)
        {
            var res = new AnArray();
            foreach(var s in m_KnownConfigurationIssues)
            {
                res.Add(s);
            }
            var mres = new Map
            {
                ["issues"] = res
            };
            SuccessResponse(req, mres);
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        private void ShowServerParams(HttpRequest req, Map jsondata)
        {
            var res = new AnArray();
            var resList = new Dictionary<string, ServerParamAttribute>();
            foreach (var kvp in m_Loader.ServerParams)
            {
                ServerParamAttribute paraType;
                if (!resList.TryGetValue(kvp.Key, out paraType) || paraType.Type == ServerParamType.GlobalOnly)
                {
                    resList[kvp.Key] = kvp.Value;
                }
            }

            foreach (var kvp in resList)
            {
                var eres = new Map
                {
                    { "name", kvp.Key }
                };
                if (!string.IsNullOrEmpty(kvp.Value.Description))
                {
                    eres.Add("description", kvp.Value.Description);
                }
                eres.Add("type", kvp.Value.Type.ToString());
                var paraType = kvp.Value.ParameterType;
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
            var mres = new Map
            {
                ["serverparams"] = res
            };
            SuccessResponse(req, mres);
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        private void SetServerParam(HttpRequest req, Map jsondata)
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

                var parameter = jsondata["parameter"].ToString();
                var value = jsondata["value"].ToString();
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
        private void GetServerParamsExplicitly(HttpRequest req, Map jsondata)
        {
            IValue ipara;
            AnArray paradata;
            if (!jsondata.TryGetValue("parameters", out ipara) ||
                (paradata = ipara as AnArray) == null)
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                var resultlist = new AnArray();
                foreach (var iv in paradata)
                {
                    var reqdata = iv as Map;
                    if (reqdata == null || !reqdata.ContainsKey("parameter"))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    UUID regionid = UUID.Zero;
                    if (reqdata.ContainsKey("regionid") && !UUID.TryParse(reqdata["regionid"].ToString(), out regionid))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    string parameter = reqdata["parameter"].ToString();
                    string value;
                    if (parameter.StartsWith("WebIF.Admin.User."))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }

                    if(m_ServerParams.TryGetExplicitValue(regionid, parameter, out value))
                    {
                        var entry = new Map
                        {
                            { "parameter", parameter },
                            { "value", value },
                            { "regionid", regionid }
                        };
                        resultlist.Add(entry);
                    }
                }
                var res = new Map
                {
                    ["values"] = resultlist
                };
                SuccessResponse(req, res);
            }
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        private void GetServerParams(HttpRequest req, Map jsondata)
        {
            IValue ipara;
            AnArray paradata;
            if(!jsondata.TryGetValue("parameters", out ipara) ||
                (paradata = ipara as AnArray) == null)
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                var resultlist = new AnArray();
                foreach(var iv in paradata)
                {
                    var reqdata = iv as Map;
                    if(reqdata == null || !reqdata.ContainsKey("parameter"))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    var regionid = UUID.Zero;
                    if (reqdata.ContainsKey("regionid") && !UUID.TryParse(reqdata["regionid"].ToString(), out regionid))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }
                    var parameter = reqdata["parameter"].ToString();
                    string value;
                    if (parameter.StartsWith("WebIF.Admin.User."))
                    {
                        ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                        return;
                    }

                    if (m_ServerParams.TryGetExplicitValue(regionid, parameter, out value))
                    {
                        var entry = new Map
                        {
                            { "parameter", parameter },
                            { "value", value },
                            { "regionid", regionid }
                        };
                        resultlist.Add(entry);
                    }
                    else if (m_ServerParams.TryGetExplicitValue(UUID.Zero, parameter, out value))
                    {
                        var entry = new Map
                        {
                            { "parameter", parameter },
                            { "value", value },
                            { "regionid", UUID.Zero }
                        };
                        resultlist.Add(entry);
                    }
                }
                var res = new Map
                {
                    ["values"] = resultlist
                };
                SuccessResponse(req, res);
            }
        }

        [AdminWebIfRequiredRight("serverparams.manage")]
        private void GetServerParam(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("parameter"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                var regionid = UUID.Zero;
                if (jsondata.ContainsKey("regionid") && !UUID.TryParse(jsondata["regionid"].ToString(), out regionid))
                {
                    ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }

                var parameter = jsondata["parameter"].ToString();
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
                var res = new Map
                {
                    { "parameter", parameter }
                };

                if (m_ServerParams.TryGetExplicitValue(regionid, parameter, out value))
                {
                    res.Add("value", value);
                    res.Add("regionid", regionid);
                }
                else if (m_ServerParams.TryGetExplicitValue(UUID.Zero, parameter, out value))
                {
                    res.Add("value", value);
                    res.Add("regionid", UUID.Zero);
                }
                else
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                    return;
                }

                SuccessResponse(req, res);
            }
        }

        [AdminWebIfRequiredRight("webif.admin.users.manage")]
        private void GrantRight(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user") || !jsondata.ContainsKey("right"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                var userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";
                string pass_sha1;
                string rights;

                if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                    m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
                {
                    string[] rightlist = rights.ToLower().Split(',');
                    var rightlistnew = new List<string>();
                    var resdata = new AnArray();
                    foreach(var r in rightlist)
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
                    var m = new Map
                    {
                        ["user"] = jsondata["user"],
                        ["rights"] = resdata
                    };
                    SuccessResponse(req, m);
                }
                else
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                }
            }
        }

        [AdminWebIfRequiredRight("webif.admin.users.manage")]
        private void RevokeRight(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user") || !jsondata.ContainsKey("right"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                var userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";
                string pass_sha1;
                string rights;

                if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                    m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
                {
                    var rightlist = rights.ToLower().Split(',');
                    var rightlistnew = new List<string>();
                    var resdata = new AnArray();
                    foreach (var r in rightlist)
                    {
                        var trimmed = r.Trim();
                        if (trimmed != jsondata["right"].ToString())
                        {
                            rightlistnew.Add(r.Trim());
                            resdata.Add(r.Trim());
                        }
                    }
                    m_ServerParams[UUID.Zero, userRef + "Rights"] = string.Join(",", rightlistnew);
                    var m = new Map
                    {
                        ["user"] = jsondata["user"],
                        ["rights"] = resdata
                    };
                    SuccessResponse(req, m);
                }
                else
                {
                    ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                }
            }
        }

        [AdminWebIfRequiredRight("webif.admin.users.manage")]
        private void DeleteUser(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user"))
            {
                ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            else
            {
                var userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";

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
}
