// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.ServiceInterfaces.Teleport;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.ServiceInterfaces.Traveling;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Friends;
using SilverSim.Types.Grid;
using SilverSim.Types.GridUser;
using SilverSim.Types.Inventory;
using SilverSim.Types.Presence;
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.XmlRpc;
using SilverSim.Types.TravelingData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace SilverSim.Grid.Login
{
    #region Service Implementation
    [Description("XmlRpc Login Handler")]
    [ServerParam("AllowLoginViaHttpWhenHttpsIsConfigured", ParameterType = typeof(bool), Type = ServerParamType.GlobalOnly)]
    [ServerParam("WelcomeMessage", ParameterType = typeof(string), Type = ServerParamType.GlobalOnly)]
    [ServerParam("GridLibraryOwner", ParameterType = typeof(UUID), Type = ServerParamType.GlobalOnly)]
    [ServerParam("GridLibraryFolderId", ParameterType = typeof(UUID), Type = ServerParamType.GlobalOnly)]
    [ServerParam("GridLibraryEnabled", ParameterType = typeof(bool), Type = ServerParamType.GlobalOnly)]
    [ServerParam("AboutPage", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("WelcomePage", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("RegisterPage", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("GridNick", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("GridName", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("AllowMultiplePresences", ParameterType = typeof(bool), Type = ServerParamType.GlobalOnly)]
    [ServerParam("MaxAgentGroups", ParameterType = typeof(uint), Type = ServerParamType.GlobalOnly)]
    public class XmlRpcLoginHandler : IPlugin, IServerParamListener
    {
        private static readonly ILog m_Log = LogManager.GetLogger("XMLRPC LOGIN");

        BaseHttpServer m_HttpServer;
        BaseHttpServer m_HttpsServer;
        HttpXmlRpcHandler m_XmlRpcServer;
        RwLockedList<string> m_ConfigurationIssues;
        bool m_AllowLoginViaHttpWhenHttpsIsConfigured;

        UserAccountServiceInterface m_UserAccountService;
        GridUserServiceInterface m_GridUserService;
        GridServiceInterface m_GridService;
        InventoryServiceInterface m_InventoryService;
        PresenceServiceInterface m_PresenceService;
        FriendsServiceInterface m_FriendsService;
        AuthInfoServiceInterface m_AuthInfoService;
        AvatarServiceInterface m_AvatarService;
        TravelingDataServiceInterface m_TravelingDataService;
        ILoginConnectorServiceInterface m_LoginConnectorService;

        string m_UserAccountServiceName;
        string m_GridUserServiceName;
        string m_GridServiceName;
        string m_InventoryServiceName;
        string m_PresenceServiceName;
        string m_FriendsServiceName;
        string m_AuthInfoServiceName;
        string m_AvatarServiceName;
        string m_TravelingDataServiceName;
        string m_LoginConnectorServiceName;
        UUID m_GridLibraryOwner = new UUID("11111111-1111-0000-0000-000100bba000");
        UUID m_GridLibaryFolderId = new UUID("00000112-000f-0000-0000-000100bba000");
        string m_WelcomeMessage = "Greetings Programs";
        Uri m_AboutPage;
        Uri m_WelcomePage;
        Uri m_RegisterPage;
        bool m_GridLibraryEnabled;
        string m_GridNick = string.Empty;
        string m_GridName = string.Empty;
        bool m_AllowMultiplePresences;
        int m_MaxAgentGroups = 42;
        string m_HomeUri;
        string m_GatekeeperUri;
        List<IServiceURLsGetInterface> m_ServiceURLsGetters = new List<IServiceURLsGetInterface>();

        public XmlRpcLoginHandler(IConfig ownSection)
        {
            m_UserAccountServiceName = ownSection.GetString("UserAccountService", "UserAccountService");
            m_GridUserServiceName = ownSection.GetString("GridUserService", "GridUserService");
            m_GridServiceName = ownSection.GetString("GridService", "GridService");
            m_InventoryServiceName = ownSection.GetString("InventoryService", "InventoryService");
            m_PresenceServiceName = ownSection.GetString("PresenceService", "PresenceService");
            m_FriendsServiceName = ownSection.GetString("FriendsService", "FriendsService");
            m_AvatarServiceName = ownSection.GetString("AvatarService", "AvatarService");
            m_AuthInfoServiceName = ownSection.GetString("AuthInfoService", "AuthInfoService");
            m_TravelingDataServiceName = ownSection.GetString("TravelingDataService", "TravelingDataService");
            m_LoginConnectorServiceName = ownSection.GetString("LoginConnectorService", "LoginConnectorService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ServiceURLsGetters = loader.GetServicesByValue<IServiceURLsGetInterface>();
            m_HomeUri = loader.HomeURI;
            m_XmlRpcServer = loader.XmlRpcServer;
            m_GatekeeperUri = loader.GatekeeperURI;
            m_UserAccountService = loader.GetService<UserAccountServiceInterface>(m_UserAccountServiceName);
            m_GridUserService = loader.GetService<GridUserServiceInterface>(m_GridUserServiceName);
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            m_InventoryService = loader.GetService<InventoryServiceInterface>(m_InventoryServiceName);
            m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
            m_FriendsService = loader.GetService<FriendsServiceInterface>(m_FriendsServiceName);
            m_AuthInfoService = loader.GetService<AuthInfoServiceInterface>(m_AuthInfoServiceName);
            m_AvatarService = loader.GetService<AvatarServiceInterface>(m_AvatarServiceName);
            m_AuthInfoService = loader.GetService<AuthInfoServiceInterface>(m_AuthInfoServiceName);
            m_TravelingDataService = loader.GetService<TravelingDataServiceInterface>(m_TravelingDataServiceName);
            m_LoginConnectorService = loader.GetService<ILoginConnectorServiceInterface>(m_LoginConnectorServiceName);

            m_ConfigurationIssues = loader.KnownConfigurationIssues;
            m_HttpServer = loader.HttpServer;
            try
            {
                m_HttpsServer = loader.HttpsServer;
            }
            catch
            {
                m_HttpsServer = null;
            }

            m_HttpServer.UriHandlers.Add("/login", HandleLogin);
            m_HttpServer.UriHandlers.Add("/get_grid_info", HandleGetGridInfo);
            m_HttpServer.UriHandlers.Add("/json_grid_info", HandleJsonGridInfo);
            if (null != m_HttpsServer)
            {
                m_HttpsServer.UriHandlers.Add("/login", HandleLogin);
                m_HttpsServer.UriHandlers.Add("/get_grid_info", HandleGetGridInfo);
                m_HttpsServer.UriHandlers.Add("/json_grid_info", HandleJsonGridInfo);
            }
            m_XmlRpcServer.XmlRpcMethods.Add("login_to_simulator", HandleLogin);
        }

        Dictionary<string, string> CollectGridInfo()
        {
            Dictionary<string, string> list = new Dictionary<string, string>();
            list.Add("platform", "SilverSim");
            if (m_HttpsServer != null && !m_AllowLoginViaHttpWhenHttpsIsConfigured)
            {
                list.Add("login", m_HttpsServer.ServerURI);
            }
            else
            {
                list.Add("login", m_HttpServer.ServerURI);
            }
            if (m_AboutPage != null)
            {
                list.Add("about", m_AboutPage.ToString());
            }
            if (m_RegisterPage != null)
            {
                list.Add("register", m_RegisterPage.ToString());
            }
            if (m_WelcomePage != null)
            {
                list.Add("welcome", m_WelcomePage.ToString());
            }
            list.Add("gridnick", m_GridNick);
            list.Add("gridname", m_GridName);
            return list;
        }

        void HandleJsonGridInfo(HttpRequest httpreq)
        {
            Map jsonres = new Map();
            foreach (KeyValuePair<string, string> kvp in CollectGridInfo())
            {
                jsonres.Add(kvp.Key, kvp.Value);
            }

            using (HttpResponse res = httpreq.BeginResponse("application/json-rpc"))
            {
                using (Stream s = res.GetOutputStream())
                {
                    Json.Serialize(jsonres, s);
                }
            }
        }

        void HandleGetGridInfo(HttpRequest httpreq)
        {
            using (HttpResponse res = httpreq.BeginResponse("text/xml"))
            {
                using (XmlTextWriter writer = res.GetOutputStream().UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("gridinfo");
                    foreach(KeyValuePair<string, string> kvp in CollectGridInfo())
                    {
                        writer.WriteNamedValue(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndElement();
                }
            }
        }

        class LoginData
        {
            static Random m_RandomNumber = new Random();
            static object m_RandomNumberLock = new object();

            private static uint NewCircuitCode
            {
                get
                {
                    int rand;
                    lock (m_RandomNumberLock)
                    {
                        rand = m_RandomNumber.Next(1, Int32.MaxValue);
                    }
                    return (uint)rand;
                }
            }

            public ClientInfo ClientInfo = new ClientInfo();
            public SessionInfo SessionInfo = new SessionInfo();
            public UserAccount Account;
            public DestinationInfo DestinationInfo = new DestinationInfo();
            public CircuitInfo CircuitInfo = new CircuitInfo();
            public readonly List<string> LoginOptions = new List<string>();
            public List<InventoryFolder> InventorySkeleton;
            public List<InventoryFolder> InventoryLibSkeleton;
            public List<InventoryItem> ActiveGestures;
            public bool HaveGridLibrary;
            public InventoryFolder InventoryRoot;
            public InventoryFolder InventoryLibRoot;
            public List<FriendInfo> Friends;
            public bool HaveAppearance;
            public AppearanceInfo AppearanceInfo;

            public LoginData()
            {
                CircuitInfo.CapsPath = UUID.Random.ToString();
                CircuitInfo.CircuitCode = NewCircuitCode;
            }
        }

        UUID Authenticate(UUID avatarid, UUID sessionid, string passwd)
        {
            try
            {
                return m_AuthInfoService.Authenticate(sessionid, avatarid, passwd, 30);
            }
            catch(AuthenticationFailedException)
            {
                throw new LoginFailResponseException("key", "Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.");
            }
        }

        void HandleLogin(HttpRequest httpreq)
        {
            if (!m_AllowLoginViaHttpWhenHttpsIsConfigured && m_HttpsServer != null && !httpreq.IsSsl)
            {
                HandleLoginRedirect(httpreq);
                return;
            }

            /* no LSL requests allowed here */
            if (httpreq.ContainsHeader("X-SecondLife-Shard"))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                m_Log.ErrorFormat("Received request with X-SecondLife-Shard from {0}", httpreq.CallerIP);
                return;
            }

            XmlRpc.XmlRpcRequest req;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                m_Log.ErrorFormat("Received wrong method request from {0}", httpreq.CallerIP);
                return;
            }
            try
            {
                req = XmlRpc.DeserializeRequest(httpreq.Body);
                req.IsSsl = httpreq.IsSsl;
                req.CallerIP = httpreq.CallerIP;
            }
            catch
            {
                m_Log.ErrorFormat("Deserialization of login request from {0} failed", httpreq.CallerIP);
                using (HttpResponse res = httpreq.BeginResponse("text/xml"))
                {
                    using (Stream s = res.GetOutputStream())
                    {
                        new XmlRpc.XmlRpcFaultResponse(-32700, "Invalid XML RPC Request").Serialize(s);
                    }
                }
                return;
            }

            using (HttpResponse res = httpreq.BeginResponse("text/xml"))
            {
                using (Stream s = res.GetOutputStream())
                {
                    HandleLogin(req).Serialize(s);
                }
            }
        }
        XmlRpc.XmlRpcResponse HandleLogin(XmlRpc.XmlRpcRequest req)
        {
            if (!m_AllowLoginViaHttpWhenHttpsIsConfigured && m_HttpsServer != null && !req.IsSsl)
            {
                throw new HttpRedirectKeepVerbException(m_HttpsServer + "login");
            }

            if (req.Params.Count != 1)
            {
                
                m_Log.ErrorFormat("Request from {0} does not contain a single struct parameter", req.CallerIP);
                throw new XmlRpc.XmlRpcFaultException(4, "Missing struct parameter");
            }

            Map structParam = req.Params[0] as Map;
            if(null == structParam)
            {
                m_Log.ErrorFormat("Request from {0} does not contain struct parameter", req.CallerIP);
                throw new XmlRpc.XmlRpcFaultException(4, "Missing struct parameter");
            }

            Dictionary<string, string> loginParams = new Dictionary<string, string>();
            foreach (string reqparam in RequiredParameters)
            {
                if (!structParam.ContainsKey(reqparam))
                {
                    m_Log.ErrorFormat("Request from {0} does not contain a parameter {1}", req.CallerIP, reqparam);
                    throw new XmlRpc.XmlRpcFaultException(4, "Missing parameter " + reqparam);
                }
                loginParams.Add(reqparam, structParam[reqparam].ToString());
            }

            LoginData loginData = new LoginData();
            string firstName = loginParams["first"];
            string lastName = loginParams["last"];
            string startLocation = loginParams["start"];
            string passwd = loginParams["passwd"];
            loginData.ClientInfo.Channel = loginParams["channel"];
            loginData.ClientInfo.ClientVersion = loginParams["version"];
            loginData.ClientInfo.Mac = loginParams["mac"];
            loginData.ClientInfo.ID0 = loginParams["id0"];
            loginData.ClientInfo.ClientIP = req.CallerIP;
            loginData.DestinationInfo.StartLocation = loginParams["start"];

            UUID scopeId = UUID.Zero;
            IValue iv;
            if(structParam.TryGetValue("scope_id", out iv))
            {
                if(!UUID.TryParse(iv.ToString(), out scopeId))
                {
                    m_Log.ErrorFormat("Request from {0} does not contain invalid parameter scope_id", req.CallerIP);
                    throw new XmlRpc.XmlRpcFaultException(4, "Invalid parameter scope_id");
                }
            }

            try
            {
                loginData.Account = m_UserAccountService[scopeId, firstName, lastName];
            }
            catch
            {
                m_Log.ErrorFormat("Request from {0} does not reference a known account {2} {3} (Scope {1})", req.CallerIP, scopeId, firstName, lastName);
                return LoginFailResponse("key", "Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.");
            }
            if (scopeId != loginData.Account.ScopeID && loginData.Account.ScopeID != UUID.Zero)
            {
                m_Log.ErrorFormat("Request from {0} does not reference a valid account {2} {3} (Scope {1})", req.CallerIP, scopeId, firstName, lastName);
                return LoginFailResponse("key", "Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.");
            }
            if(loginData.Account.UserLevel < 0)
            {
                m_Log.ErrorFormat("Request from {0} does not reference an enabled account {2} {3} (Scope {1})", req.CallerIP, scopeId, firstName, lastName);
                return LoginFailResponse("key", "Could not authenticate your avatar. Account has been disabled.");
            }
            loginData.Account.Principal.HomeURI = new Uri(m_HomeUri, UriKind.Absolute);

            AnArray optarray = null;
            if (structParam.TryGetValue<AnArray>("options", out optarray))
            {
                foreach (IValue ivopt in optarray)
                {
                    loginData.LoginOptions.Add(ivopt.ToString());
                }
            }

            loginData.SessionInfo.SessionID = UUID.Random;

            try
            {
                loginData.SessionInfo.SecureSessionID = Authenticate(loginData.Account.Principal.ID, loginData.SessionInfo.SessionID, passwd);
            }
            catch
            {
                m_Log.ErrorFormat("Request from {0} failed to authenticate account {2} {3} (Scope {1})", req.CallerIP, scopeId, firstName, lastName);
                return LoginFailResponse("key", "Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.");
            }

            loginData.HaveAppearance = m_AvatarService.TryGetAppearanceInfo(loginData.Account.Principal.ID, out loginData.AppearanceInfo);

            // After authentication, we have to remove the token if something fails
            try
            {
                return LoginAuthenticated(req, loginData);
            }
            catch(LoginFailResponseException e)
            {
                m_AuthInfoService.ReleaseToken(loginData.Account.Principal.ID, loginData.SessionInfo.SecureSessionID);
#if DEBUG
                m_Log.ErrorFormat("Request from {0} failed to process.\nException {1}: {2}\n{3}", req.CallerIP, e.GetType().FullName, e.Message, e.StackTrace);
#endif
                return LoginFailResponse(e.Reason, e.Message);
            }
            catch(Exception e)
            {
                m_Log.Debug("Unexpected error occured", e);
                throw new LoginFailResponseException("key", "Unexpected errror occured");
            }
        }

        XmlRpc.XmlRpcResponse LoginAuthenticated(XmlRpc.XmlRpcRequest req, LoginData loginData)
        {
            try
            {
                m_InventoryService.CheckInventory(loginData.Account.Principal.ID);
            }
            catch
            {
                throw new LoginFailResponseException("key", "The inventory service is not responding.  Please notify your grid administrator (A)");
            }

            if(loginData.LoginOptions.Contains(Option_InventoryRoot))
            {
                try
                {
                    loginData.InventoryRoot = m_InventoryService.Folder[loginData.Account.Principal.ID, AssetType.RootFolder];
                }
                catch
                {
                    throw new LoginFailResponseException("key", "The inventory service is not responding.  Please notify your grid administrator (B1)");
                }
            }

            if (loginData.LoginOptions.Contains(Option_InventorySkeleton))
            {
                try
                {
                    loginData.InventorySkeleton = m_InventoryService.GetInventorySkeleton(loginData.Account.Principal.ID);
                }
                catch
                {
                    throw new LoginFailResponseException("key", "The inventory service is not responding.  Please notify your grid administrator (B2)");
                }
            }

            if (m_GridLibraryEnabled && m_GridLibraryOwner != UUID.Zero && m_GridLibaryFolderId != UUID.Zero)
            {
                loginData.HaveGridLibrary = true;
                if(loginData.LoginOptions.Contains(Option_InventoryLibRoot))
                {
                    try
                    {
                        loginData.InventoryLibRoot = m_InventoryService.Folder[m_GridLibraryOwner, m_GridLibaryFolderId];
                    }
                    catch
                    {
                        throw new LoginFailResponseException("key", "The inventory service is not responding.  Please notify your grid administrator (C1)");
                    }
                }

                if (loginData.LoginOptions.Contains(Option_inventoryLibSkeleton))
                {
                    try
                    {
                        loginData.InventoryLibSkeleton = m_InventoryService.GetInventorySkeleton(m_GridLibraryOwner);
                    }
                    catch
                    {
                        throw new LoginFailResponseException("key", "The inventory service is not responding.  Please notify your grid administrator (C2)");
                    }
                }
            }

            if (loginData.LoginOptions.Contains(Option_Gestures))
            {
                try
                {
                    loginData.ActiveGestures = m_InventoryService.GetActiveGestures(loginData.Account.Principal.ID);
                }
                catch
                {
                    throw new LoginFailResponseException("key", "The inventory service is not responding.  Please notify your grid administrator (D)");
                }
            }

            if (loginData.LoginOptions.Contains(Option_BuddyList))
            {
                try
                {
                    loginData.Friends = m_FriendsService[loginData.Account.Principal];
                }
                catch(Exception e)
                {
                    m_Log.Error("Accessing friends failed", e);
                    throw new LoginFailResponseException("key", "Error accessing friends");
                }
            }

            Dictionary<string, string> avatar;
            try
            {
                avatar = m_AvatarService[loginData.Account.Principal.ID];
            }
            catch
            {
                throw new LoginFailResponseException("key", "Error accessing avatar appearance");
            }

            if (!m_AllowMultiplePresences)
            {
                try
                {
                    m_PresenceService.Remove(loginData.Account.ScopeID, loginData.Account.Principal.ID);
                }
                catch
                {
                    /* intentionally ignored */
                }
                try
                {
                    m_TravelingDataService.RemoveByAgentUUID(loginData.Account.Principal.ID);
                }
                catch
                {
                    /* intentionally ignored */
                }
            }

            PresenceInfo pInfo = new PresenceInfo();
            pInfo.UserID = loginData.Account.Principal;
            pInfo.SessionID = loginData.SessionInfo.SessionID;
            pInfo.SecureSessionID = loginData.SessionInfo.SecureSessionID;
            try
            {
                m_PresenceService[pInfo.SessionID, pInfo.UserID.ID, PresenceServiceInterface.SetType.Login] = pInfo;
            }
            catch(Exception e)
            {
                m_Log.Error("Error conntacting presence service", e);
                throw new LoginFailResponseException("key", "Error connecting to the desired location. Try connecting to another region. (A)");
            }

            try
            {
                return LoginAuthenticatedAndPresenceAdded(req, loginData);
            }
            catch
            {
                m_PresenceService[pInfo.SessionID, pInfo.UserID.ID] = null;
                throw;
            }
        }

        XmlRpc.XmlRpcResponse LoginAuthenticatedAndPresenceAdded(XmlRpc.XmlRpcRequest req, LoginData loginData)
        {
            m_GridUserService.LoggedInAdd(loginData.Account.Principal);
            try
            {
                return LoginAuthenticatedAndPresenceAndGridUserAdded(req, loginData);
            }
            catch
            {
                GridUserInfo gui;
                gui = m_GridUserService[loginData.Account.Principal];
                m_GridUserService.LoggedOut(gui.User, gui.LastRegionID, gui.LastPosition, gui.LastLookAt);
                throw;
            }
        }

        XmlRpc.XmlRpcResponse LoginAuthenticatedAndPresenceAndGridUserAdded(XmlRpc.XmlRpcRequest req, LoginData loginData)
        {
            TravelingDataInfo hgdata = new TravelingDataInfo();
            hgdata.SessionID = loginData.SessionInfo.SessionID;
            hgdata.UserID = loginData.Account.Principal.ID;
            hgdata.GridExternalName = m_GatekeeperUri;
            hgdata.ServiceToken = UUID.Random.ToString();
            hgdata.ClientIPAddress = loginData.ClientInfo.ClientIP;
            loginData.SessionInfo.ServiceSessionID = hgdata.GridExternalName + ";" + UUID.Random.ToString();

            try
            {
                m_TravelingDataService.Store(hgdata);
            }
            catch(Exception e)
            {
                m_Log.Error("Failed to store current grid location data", e);
                throw new LoginFailResponseException("key", "Error connecting to the desired location. Failed to store current grid location data.");
            }

            try
            {
                return LoginAuthenticatedAndPresenceAndGridUserAndHGTravelingDataAdded(req, loginData);
            }
            catch
            {
                m_TravelingDataService.Remove(loginData.SessionInfo.SessionID);
                throw;
            }
        }

        XmlRpc.XmlRpcResponse LoginAuthenticatedAndPresenceAndGridUserAndHGTravelingDataAdded(XmlRpc.XmlRpcRequest req, LoginData loginData)
        {
            TeleportFlags flags = TeleportFlags.None;
            if(loginData.Account.UserLevel >= 200)
            {
                flags |= TeleportFlags.Godlike;
            }

            foreach(IServiceURLsGetInterface getter in m_ServiceURLsGetters)
            {
                getter.GetServiceURLs(loginData.Account.ServiceURLs);
            }

            string seedCapsURI;
            try
            {
                m_LoginConnectorService.LoginTo(loginData.Account, loginData.ClientInfo, loginData.SessionInfo, loginData.DestinationInfo, loginData.CircuitInfo, loginData.AppearanceInfo, flags, out seedCapsURI);
            }
            catch(Exception e)
            {
                m_Log.Error("Login to simulator failed", e);
                throw new LoginFailResponseException("key", e.Message);
            }

            XmlRpc.XmlRpcResponse res = new XmlRpc.XmlRpcResponse();
            Map resStruct = new Map();
            res.ReturnValue = resStruct;
            resStruct.Add("look_at", string.Format(CultureInfo.InvariantCulture, "[r{0},r{1},r{2}]", loginData.DestinationInfo.LookAt.X, loginData.DestinationInfo.LookAt.Y, loginData.DestinationInfo.LookAt.Z));
            resStruct.Add("agent_access_max", "A");
            resStruct.Add("max-agent-groups", m_MaxAgentGroups);
            resStruct.Add("seed_capability", seedCapsURI);
            resStruct.Add("region_x", loginData.DestinationInfo.Location.X);
            resStruct.Add("region_y", loginData.DestinationInfo.Location.Y);
            resStruct.Add("region_size_x", loginData.DestinationInfo.Size.X);
            resStruct.Add("region_size_y", loginData.DestinationInfo.Size.Y);
            resStruct.Add("circuit_code", (int)loginData.CircuitInfo.CircuitCode);
            if(loginData.InventoryRoot != null)
            {
                Map data = new Map();
                data.Add("folder_id", loginData.InventoryRoot.ID);
                AnArray ardata = new AnArray();
                ardata.Add(data);
                resStruct.Add("inventory-root", ardata);
            }

            if(loginData.LoginOptions.Contains(Option_LoginFlags))
            {
                Map loginFlags = new Map();
                loginFlags.Add("stipend_since_login", "N");
                loginFlags.Add("ever_logged_in", loginData.Account.IsEverLoggedIn ? "Y" : "N");
                loginFlags.Add("gendered", loginData.HaveAppearance ? "Y" : "N");
                loginFlags.Add("daylight_savings", "N");
                resStruct.Add("login-flags", loginFlags);
            }

            resStruct.Add("WelcomeMessage", m_WelcomeMessage);

            if(loginData.InventoryLibRoot != null)
            {
                Map data = new Map();
                data.Add("folder_id", loginData.InventoryLibRoot.ID);
                AnArray ardata = new AnArray();
                ardata.Add(data);
                resStruct.Add("inventory-lib-root", ardata);
            }

            resStruct.Add("first_name", loginData.Account.Principal.FirstName);
            resStruct.Add("seconds_since_epoch", Date.GetUnixTime());

            if(loginData.LoginOptions.Contains(Option_UiConfig))
            {
                Map uic = new Map();
                uic.Add("allow_first_life", "Y");
                resStruct.Add("ui-config", uic);
            }

            if(loginData.LoginOptions.Contains(Option_EventCategories))
            {
                resStruct.Add("event_categories", new AnArray());
            }

            if(loginData.LoginOptions.Contains(Option_ClassifiedCategories))
            {
                AnArray categorylist = new AnArray();
                Map categorydata;

                categorydata = new Map();
                categorydata.Add("category_name", "Shopping");
                categorydata.Add("category_id", 1);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Land Rental");
                categorydata.Add("category_id", 2);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Property Rental");
                categorydata.Add("category_id", 3);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Special Attention");
                categorydata.Add("category_id", 4);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "New Products");
                categorydata.Add("category_id", 5);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Employment");
                categorydata.Add("category_id", 6);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Wanted");
                categorydata.Add("category_id", 7);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Service");
                categorydata.Add("category_id", 8);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Personal");
                categorydata.Add("category_id", 9);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Shopping");
                categorydata.Add("category_id", 1);
                categorylist.Add(categorydata);

                categorydata = new Map();
                categorydata.Add("category_name", "Shopping");
                categorydata.Add("category_id", 1);
                categorylist.Add(categorydata);

                resStruct.Add("classified_categories", categorylist);
            }

            if(loginData.InventorySkeleton != null)
            {
                AnArray folderArray = new AnArray();
                foreach(InventoryFolder folder in loginData.InventorySkeleton)
                {
                    Map folderData = new Map();
                    folderData.Add("folder_id", folder.ID);
                    folderData.Add("parent_id", folder.ParentFolderID);
                    folderData.Add("name", folder.Name);
                    folderData.Add("type_default", (int)folder.InventoryType);
                    folderData.Add("version", folder.Version);
                    folderArray.Add(folderData);
                }
                resStruct.Add("inventory-skeleton", folderArray);
            }

            resStruct.Add("sim_ip", ((IPEndPoint)loginData.DestinationInfo.SimIP).Address.ToString());
            resStruct.Add("map-server-url", loginData.CircuitInfo.MapServerURL);

            if(loginData.Friends != null)
            {
                AnArray friendsArray = new AnArray();
                foreach(FriendInfo fi in loginData.Friends)
                {
                    Map friendData = new Map();
                    friendData.Add("buddy_id", fi.Friend.ID);
                    friendData.Add("buddy_rights_given", (int)fi.UserGivenFlags);
                    friendData.Add("buddy_rights_has", (int)fi.FriendGivenFlags);
                    friendsArray.Add(friendData);
                }
                resStruct.Add("buddy-list", friendsArray);
            }

            if(loginData.ActiveGestures != null)
            {
                AnArray gestureArray = new AnArray();
                foreach(InventoryItem item in loginData.ActiveGestures)
                {
                    Map gestureData = new Map();
                    gestureData.Add("asset_id", item.AssetID);
                    gestureData.Add("item_id", item.ID);
                    gestureArray.Add(gestureData);
                }
                resStruct.Add("gestures", gestureArray);
            }

            resStruct.Add("http_port", loginData.DestinationInfo.ServerHttpPort);
            resStruct.Add("sim_port", loginData.DestinationInfo.ServerPort);
            resStruct.Add("start_location", loginData.DestinationInfo.StartLocation);

            if(loginData.HaveGridLibrary && loginData.LoginOptions.Contains(Option_InventoryLibOwner))
            {
                Map data = new Map();
                data.Add("agent_id", m_GridLibraryOwner);
                AnArray ar = new AnArray();
                ar.Add(data);
                resStruct.Add("inventory-lib-owner", ar);
            }

            Map initial_outfit_data = new Map();
            initial_outfit_data.Add("folder_name", "Nightclub Female");
            initial_outfit_data.Add("gender", "female");
            AnArray initial_outfit_array = new AnArray();
            initial_outfit_array.Add(initial_outfit_data);
            resStruct.Add("initial-outfit", initial_outfit_array);

            if(loginData.InventoryLibSkeleton != null)
            {
                AnArray folderArray = new AnArray();
                foreach(InventoryFolder folder in loginData.InventoryLibSkeleton)
                {
                    Map folderData = new Map();
                    folderData.Add("folder_id", folder.ID);
                    folderData.Add("parent_id", folder.ParentFolderID);
                    folderData.Add("name", folder.Name);
                    folderData.Add("type_default", (int)folder.InventoryType);
                    folderData.Add("version", folder.Version);
                    folderArray.Add(folderData);
                }
                resStruct.Add("inventory-skel-lib", folderArray);
            }

            resStruct.Add("session_id", loginData.SessionInfo.SessionID);
            resStruct.Add("agent_id", loginData.Account.Principal.ID);

            if(loginData.LoginOptions.Contains(Option_EventNotifications))
            {
                resStruct.Add("event_notifications", new AnArray());
            }

            Map globalTextureData = new Map();
            globalTextureData.Add("cloud_texture_id", "dc4b9f0b-d008-45c6-96a4-01dd947ac621");
            globalTextureData.Add("sun_texture_id", "cce0f112-878f-4586-a2e2-a8f104bba271");
            globalTextureData.Add("moon_texture_id", "ec4b9f0b-d008-45c6-96a4-01dd947ac621");

            resStruct.Add("global-textures", globalTextureData);

            resStruct.Add("login", "true");
            resStruct.Add("agent_access", "M");
            resStruct.Add("secure_session_id", loginData.SessionInfo.SecureSessionID);
            resStruct.Add("last_name", loginData.Account.Principal.LastName);

            if (!loginData.Account.IsEverLoggedIn)
            {
                try
                {
                    m_UserAccountService.SetEverLoggedIn(loginData.Account.ScopeID, loginData.Account.Principal.ID);
                }
                catch
                {
                    /* intentionally ignored */
                }
            }

            return res;
        }

        const string Option_InventoryRoot = "inventory-root";
        const string Option_InventorySkeleton = "inventory-skeleton";
        const string Option_InventoryLibRoot = "inventory-lib-root";
        const string Option_InventoryLibOwner = "inventory-lib-owner";
        const string Option_inventoryLibSkeleton = "inventory-skel-lib";
        const string Option_Gestures = "gestures";
        const string Option_EventCategories = "event_categories";
        const string Option_EventNotifications = "event_notifications";
        const string Option_ClassifiedCategories = "classified_categories";
        const string Option_BuddyList = "buddy-list";
        const string Option_UiConfig = "ui-config";
        const string Option_LoginFlags = "login-flags";
        const string Option_GlobalTextures = "global-textures";
        const string Option_AdultCompliant = "adult_compliant";


        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
        readonly string[] RequiredParameters = new string[] { "first", "last", "start", "passwd", "channel", "version", "mac", "id0" };

        class LoginFailResponseException : Exception
        {
            public string Reason { get; private set; }

            public LoginFailResponseException(string reason, string message)
                : base(message)
            {
                Reason = reason;
            }
        }

        XmlRpc.XmlRpcResponse LoginFailResponse(string reason, string message)
        {
            XmlRpc.XmlRpcResponse res = new XmlRpc.XmlRpcResponse();
            Map m = new Map();
            m.Add("reason", reason);
            m.Add("message", message);
            m.Add("login", false);
            res.ReturnValue = m;
            return res;
        }

        public void HandleLoginRedirect(HttpRequest httpreq)
        {
            using (HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.RedirectKeepVerb, "Permanently moved"))
            {
                httpres.Headers.Add("Location", m_HttpsServer.ServerURI + "login");
            }
        }

        #region Server Parameters
        readonly object m_ConfigUpdateLock = new object();

        const string ConfigIssueText = "Server parameter \"AllowLoginViaHttpWhenHttpsIsConfigured\" is set to true. Please disable it.";
        [ServerParam("AllowLoginViaHttpWhenHttpsIsConfigured")]
        public void HandleAllowLoginViaHttpWhenHttpsIsConfigured(UUID regionid, string value)
        {
            if(regionid != UUID.Zero)
            {
                return;
            }
            bool val;
            lock (m_ConfigUpdateLock)
            {
                if (bool.TryParse(value, out val))
                {
                    m_AllowLoginViaHttpWhenHttpsIsConfigured = val;
                }
                else
                {
                    m_AllowLoginViaHttpWhenHttpsIsConfigured = false;
                }
                if (m_AllowLoginViaHttpWhenHttpsIsConfigured)
                {
                    m_ConfigurationIssues.AddIfNotExists(ConfigIssueText);
                }
                else
                {
                    m_ConfigurationIssues.Remove(ConfigIssueText);
                }
            }
        }

        [ServerParam("MaxAgentGroups")]
        public void HandleMaxAgentGroups(UUID regionid, string value)
        {
            if(regionid != UUID.Zero)
            {
                return;
            }
            int val;
            if(string.IsNullOrEmpty(value) || !int.TryParse(value, out val))
            {
                m_MaxAgentGroups = 42;
            }
            else
            {
                m_MaxAgentGroups = val;
            }
        }

        [ServerParam("WelcomeMessage")]
        public void HandleWelcomeMessage(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            m_WelcomeMessage = value;
        }
        [ServerParam("GridLibraryOwner")]
        public void HandleGridLibraryOwner(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            lock (m_ConfigUpdateLock)
            {
                if(string.IsNullOrEmpty(value))
                {
                    m_GridLibraryOwner = new UUID("11111111-1111-0000-0000-000100bba000");
                }
                else if (!UUID.TryParse(value, out m_GridLibraryOwner))
                {
                    m_GridLibraryOwner = UUID.Zero;
                }
            }
        }

        [ServerParam("GridLibraryFolderId")]
        public void HandleGridLibraryFolderId(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            lock (m_ConfigUpdateLock)
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_GridLibaryFolderId = new UUID("00000112-000f-0000-0000-000100bba000");
                }
                else if (!UUID.TryParse(value, out m_GridLibaryFolderId))
                {
                    m_GridLibaryFolderId = UUID.Zero;
                }
            }
        }

        [ServerParam("GridLibraryEnabled")]
        public void HandleGridLibraryEnabled(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            lock (m_ConfigUpdateLock)
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_GridLibraryEnabled = false;
                }
                else if (!bool.TryParse(value, out m_GridLibraryEnabled))
                {
                    m_GridLibraryEnabled = false;
                }
            }
        }

        [ServerParam("AllowMultiplePresences")]
        public void HandleAllowMultiplePresences(UUID regionid, string value)
        {
            if(regionid != UUID.Zero)
            {
                return;
            }
            bool val;
            if(!bool.TryParse(value, out val))
            {
                val = false;
            }
            m_AllowMultiplePresences = val;
        }

        [ServerParam("AboutPage")]
        public void HandleAboutPage(UUID regionid, string value)
        {
            if(regionid != UUID.Zero)
            {
                return;
            }
            lock(m_ConfigUpdateLock)
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out m_AboutPage))
                {
                    m_AboutPage = null;
                }
            }
        }

        [ServerParam("WelcomePage")]
        public void HandleWelcomePage(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            lock (m_ConfigUpdateLock)
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out m_WelcomePage))
                {
                    m_WelcomePage = null;
                }
            }
        }

        [ServerParam("RegisterPage")]
        public void HandleRegisterPage(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            lock (m_ConfigUpdateLock)
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out m_RegisterPage))
                {
                    m_RegisterPage = null;
                }
            }
        }

        [ServerParam("GridName")]
        public void HandleGridName(UUID regionid, string value)
        {
            if(regionid != UUID.Zero)
            {
                return;
            }
            m_GridName = value;
        }

        [ServerParam("GridNick")]
        public void HandleGridNick(UUID regionid, string value)
        {
            if (regionid != UUID.Zero)
            {
                return;
            }
            m_GridNick = value;
        }
        #endregion
    }
    #endregion

    #region Service factory
    [PluginName("XmlRpcLoginHandler")]
    public class XmlRpcLoginHandlerFactory : IPluginFactory
    {
        public XmlRpcLoginHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new XmlRpcLoginHandler(ownSection);
        }
    }
    #endregion
}
