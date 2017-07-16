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
using System.Net;

namespace SilverSim.Grid.Login
{
    [Description("XmlRpc Login Handler")]
    [PluginName("XmlRpcLoginHandler")]
    [ServerParam("AllowLoginViaHttpWhenHttpsIsConfigured", ParameterType = typeof(bool), Type = ServerParamType.GlobalOnly, DefaultValue = false)]
    [ServerParam("WelcomeMessage", ParameterType = typeof(string), Type = ServerParamType.GlobalOnly, DefaultValue = "Welcome to your new world!")]
    [ServerParam("GridLibraryOwner", ParameterType = typeof(UUID), Type = ServerParamType.GlobalOnly, DefaultValue = "11111111-1111-0000-0000-000100bba000")]
    [ServerParam("GridLibraryFolderId", ParameterType = typeof(UUID), Type = ServerParamType.GlobalOnly, DefaultValue = "00000112-000f-0000-0000-000100bba000")]
    [ServerParam("GridLibraryEnabled", ParameterType = typeof(bool), Type = ServerParamType.GlobalOnly, DefaultValue = false)]
    [ServerParam("AboutPage", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("WelcomePage", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("RegisterPage", ParameterType = typeof(Uri), Type = ServerParamType.GlobalOnly)]
    [ServerParam("GridNick", ParameterType = typeof(string), Type = ServerParamType.GlobalOnly, DefaultValue = "")]
    [ServerParam("GridName", ParameterType = typeof(string), Type = ServerParamType.GlobalOnly, DefaultValue = "")]
    [ServerParam("AllowMultiplePresences", ParameterType = typeof(bool), Type = ServerParamType.GlobalOnly, DefaultValue = false)]
    [ServerParam("MaxAgentGroups", ParameterType = typeof(uint), Type = ServerParamType.GlobalOnly, DefaultValue = 42)]
    public class XmlRpcLoginHandler : IPlugin, IServerParamListener
    {
        private static readonly ILog m_Log = LogManager.GetLogger("XMLRPC LOGIN");

        private BaseHttpServer m_HttpServer;
        private BaseHttpServer m_HttpsServer;
        private HttpXmlRpcHandler m_XmlRpcServer;
        private RwLockedList<string> m_ConfigurationIssues;
        private bool m_AllowLoginViaHttpWhenHttpsIsConfigured;

        private UserAccountServiceInterface m_UserAccountService;
        private GridUserServiceInterface m_GridUserService;
        private GridServiceInterface m_GridService;
        private InventoryServiceInterface m_InventoryService;
        private PresenceServiceInterface m_PresenceService;
        private FriendsServiceInterface m_FriendsService;
        private AuthInfoServiceInterface m_AuthInfoService;
        private AvatarServiceInterface m_AvatarService;
        private TravelingDataServiceInterface m_TravelingDataService;
        private ILoginConnectorServiceInterface m_LoginConnectorService;

        private readonly string m_UserAccountServiceName;
        private readonly string m_GridUserServiceName;
        private readonly string m_GridServiceName;
        private readonly string m_InventoryServiceName;
        private readonly string m_PresenceServiceName;
        private readonly string m_FriendsServiceName;
        private readonly string m_AuthInfoServiceName;
        private readonly string m_AvatarServiceName;
        private readonly string m_TravelingDataServiceName;
        private readonly string m_LoginConnectorServiceName;
        private UUID m_GridLibraryOwner = new UUID("11111111-1111-0000-0000-000100bba000");
        private UUID m_GridLibaryFolderId = new UUID("00000112-000f-0000-0000-000100bba000");
        private string m_WelcomeMessage = "Welcome to your new world!";
        private Uri m_AboutPage;
        private Uri m_WelcomePage;
        private Uri m_RegisterPage;
        private bool m_GridLibraryEnabled;
        private string m_GridNick = string.Empty;
        private string m_GridName = string.Empty;
        private bool m_AllowMultiplePresences;
        private int m_MaxAgentGroups = 42;
        private string m_HomeUri;
        private string m_GatekeeperUri;
        private List<IServiceURLsGetInterface> m_ServiceURLsGetters = new List<IServiceURLsGetInterface>();
        private List<IGridInfoServiceInterface> m_GridInfoGetters = new List<IGridInfoServiceInterface>();

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
            m_GridInfoGetters = loader.GetServicesByValue<IGridInfoServiceInterface>();
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
            if (m_HttpsServer != null)
            {
                m_HttpsServer.UriHandlers.Add("/login", HandleLogin);
                m_HttpsServer.UriHandlers.Add("/get_grid_info", HandleGetGridInfo);
                m_HttpsServer.UriHandlers.Add("/json_grid_info", HandleJsonGridInfo);
            }
            m_XmlRpcServer.XmlRpcMethods.Add("login_to_simulator", HandleLogin);
        }

        private Dictionary<string, string> CollectGridInfo()
        {
            var list = new Dictionary<string, string>();

            foreach(IGridInfoServiceInterface getter in m_GridInfoGetters)
            {
                getter.GetGridInfo(list);
            }

            list["platform"] = "SilverSim";

            if (m_HttpsServer != null && !m_AllowLoginViaHttpWhenHttpsIsConfigured)
            {
                list["login"] = m_HttpsServer.ServerURI;
            }
            else
            {
                list["login"] = m_HttpServer.ServerURI;
            }
            if (m_AboutPage != null)
            {
                list["about"] = m_AboutPage.ToString();
            }
            if (m_RegisterPage != null)
            {
                list["register"] = m_RegisterPage.ToString();
            }
            if (m_WelcomePage != null)
            {
                list["welcome"] = m_WelcomePage.ToString();
            }
            list["gridnick"] = m_GridNick;
            list["gridname"] = m_GridName;
            return list;
        }

        private void HandleJsonGridInfo(HttpRequest httpreq)
        {
            var jsonres = new Map();
            foreach (KeyValuePair<string, string> kvp in CollectGridInfo())
            {
                jsonres.Add(kvp.Key, kvp.Value);
            }

            using (var res = httpreq.BeginResponse("application/json-rpc"))
            {
                using (var s = res.GetOutputStream())
                {
                    Json.Serialize(jsonres, s);
                }
            }
        }

        private void HandleGetGridInfo(HttpRequest httpreq)
        {
            using (var res = httpreq.BeginResponse("text/xml"))
            {
                using (var writer = res.GetOutputStream().UTF8XmlTextWriter())
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

        private class LoginData
        {
            private static readonly Random m_RandomNumber = new Random();
            private static readonly object m_RandomNumberLock = new object();

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

        private UUID Authenticate(UUID avatarid, UUID sessionid, string passwd)
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

        private void HandleLogin(HttpRequest httpreq)
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
                using (var res = httpreq.BeginResponse("text/xml"))
                {
                    using (var s = res.GetOutputStream())
                    {
                        new XmlRpc.XmlRpcFaultResponse(-32700, "Invalid XML RPC Request").Serialize(s);
                    }
                }
                return;
            }

            using (var res = httpreq.BeginResponse("text/xml"))
            {
                using (var s = res.GetOutputStream())
                {
                    HandleLogin(req).Serialize(s);
                }
            }
        }

        private XmlRpc.XmlRpcResponse HandleLogin(XmlRpc.XmlRpcRequest req)
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

            var structParam = req.Params[0] as Map;
            if(structParam == null)
            {
                m_Log.ErrorFormat("Request from {0} does not contain struct parameter", req.CallerIP);
                throw new XmlRpc.XmlRpcFaultException(4, "Missing struct parameter");
            }

            var loginParams = new Dictionary<string, string>();
            foreach (string reqparam in RequiredParameters)
            {
                if (!structParam.ContainsKey(reqparam))
                {
                    m_Log.ErrorFormat("Request from {0} does not contain a parameter {1}", req.CallerIP, reqparam);
                    throw new XmlRpc.XmlRpcFaultException(4, "Missing parameter " + reqparam);
                }
                loginParams.Add(reqparam, structParam[reqparam].ToString());
            }

            var loginData = new LoginData();
            string firstName = loginParams["first"];
            string lastName = loginParams["last"];
            string passwd = loginParams["passwd"];
            loginData.ClientInfo.Channel = loginParams["channel"];
            loginData.ClientInfo.ClientVersion = loginParams["version"];
            loginData.ClientInfo.Mac = loginParams["mac"];
            loginData.ClientInfo.ID0 = loginParams["id0"];
            loginData.ClientInfo.ClientIP = req.CallerIP;
            loginData.DestinationInfo.StartLocation = loginParams["start"];

            UUID scopeId = UUID.Zero;
            IValue iv;
            if(structParam.TryGetValue("scope_id", out iv) &&
                !UUID.TryParse(iv.ToString(), out scopeId))
            {
                m_Log.ErrorFormat("Request from {0} does not contain invalid parameter scope_id", req.CallerIP);
                throw new XmlRpc.XmlRpcFaultException(4, "Invalid parameter scope_id");
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
#if DEBUG
                (Exception e)
#endif
            {
                m_Log.ErrorFormat("Request from {0} failed to authenticate account {2} {3} (Scope {1})", req.CallerIP, scopeId, firstName, lastName);
#if DEBUG
                m_Log.Debug("Exception", e);
#endif
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

        private XmlRpc.XmlRpcResponse LoginAuthenticated(XmlRpc.XmlRpcRequest req, LoginData loginData)
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

            var pInfo = new PresenceInfo()
            {
                UserID = loginData.Account.Principal,
                SessionID = loginData.SessionInfo.SessionID,
                SecureSessionID = loginData.SessionInfo.SecureSessionID
            };
            try
            {
                m_PresenceService.Login(pInfo);
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
                m_PresenceService.Logout(pInfo.SessionID, pInfo.UserID.ID);
                throw;
            }
        }

        private XmlRpc.XmlRpcResponse LoginAuthenticatedAndPresenceAdded(XmlRpc.XmlRpcRequest req, LoginData loginData)
        {
            m_GridUserService.LoggedInAdd(loginData.Account.Principal);
            try
            {
                return LoginAuthenticatedAndPresenceAndGridUserAdded(req, loginData);
            }
            catch
            {
                GridUserInfo gui = m_GridUserService[loginData.Account.Principal];
                m_GridUserService.LoggedOut(gui.User, gui.LastRegionID, gui.LastPosition, gui.LastLookAt);
                throw;
            }
        }

        private XmlRpc.XmlRpcResponse LoginAuthenticatedAndPresenceAndGridUserAdded(XmlRpc.XmlRpcRequest req, LoginData loginData)
        {
            var hgdata = new TravelingDataInfo()
            {
                SessionID = loginData.SessionInfo.SessionID,
                UserID = loginData.Account.Principal.ID,
                GridExternalName = m_GatekeeperUri,
                ServiceToken = UUID.Random.ToString(),
                ClientIPAddress = loginData.ClientInfo.ClientIP
            };
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

        private XmlRpc.XmlRpcResponse LoginAuthenticatedAndPresenceAndGridUserAndHGTravelingDataAdded(XmlRpc.XmlRpcRequest req, LoginData loginData)
        {
            var flags = TeleportFlags.None;
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

            var resStruct = new Map
            {
                { "look_at", string.Format(CultureInfo.InvariantCulture, "[r{0},r{1},r{2}]", loginData.DestinationInfo.LookAt.X, loginData.DestinationInfo.LookAt.Y, loginData.DestinationInfo.LookAt.Z) },
                { "agent_access_max", "A" },
                { "max-agent-groups", m_MaxAgentGroups },
                { "seed_capability", seedCapsURI },
                { "region_x", loginData.DestinationInfo.Location.X.ToString() },
                { "region_y", loginData.DestinationInfo.Location.Y.ToString() },
                { "region_size_x", loginData.DestinationInfo.Size.X.ToString() },
                { "region_size_y", loginData.DestinationInfo.Size.Y.ToString() },
                { "circuit_code", (int)loginData.CircuitInfo.CircuitCode }
            };
            var res = new XmlRpc.XmlRpcResponse()
            {
                ReturnValue = resStruct
            };
            if (loginData.InventoryRoot != null)
            {
                var data = new Map
                {
                    ["folder_id"] = loginData.InventoryRoot.ID
                };
                var ardata = new AnArray
                {
                    data
                };
                resStruct.Add("inventory-root", ardata);
            }

            if(loginData.LoginOptions.Contains(Option_LoginFlags))
            {
                var loginFlags = new Map
                {
                    { "stipend_since_login", "N" },
                    { "ever_logged_in", loginData.Account.IsEverLoggedIn ? "Y" : "N" },
                    { "gendered", loginData.HaveAppearance ? "Y" : "N" },
                    { "daylight_savings", "N" }
                };
                resStruct.Add("login-flags", loginFlags);
            }

            resStruct.Add("WelcomeMessage", m_WelcomeMessage);

            if(loginData.InventoryLibRoot != null)
            {
                var data = new Map
                {
                    ["folder_id"] = loginData.InventoryLibRoot.ID
                };
                var ardata = new AnArray
                {
                    data
                };
                resStruct.Add("inventory-lib-root", ardata);
            }

            resStruct.Add("first_name", loginData.Account.Principal.FirstName);
            resStruct.Add("seconds_since_epoch", (int)Date.GetUnixTime());

            if(loginData.LoginOptions.Contains(Option_UiConfig))
            {
                var uic = new Map
                {
                    { "allow_first_life", "Y" }
                };
                resStruct.Add("ui-config", uic);
            }

            if(loginData.LoginOptions.Contains(Option_EventCategories))
            {
                resStruct.Add("event_categories", new AnArray());
            }

            if(loginData.LoginOptions.Contains(Option_ClassifiedCategories))
            {
                var categorylist = new AnArray();
                var categorydata = new Map
                {
                    { "category_name", "Shopping" },
                    { "category_id", 1 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "Land Rental" },
                    { "category_id", 2 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "Property Rental" },
                    { "category_id", 3 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "Special Attention" },
                    { "category_id", 4 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "New Products" },
                    { "category_id", 5 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "Employment" },
                    { "category_id", 6 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "Wanted" },
                    { "category_id", 7 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "Service" },
                    { "category_id", 8 }
                };
                categorylist.Add(categorydata);

                categorydata = new Map
                {
                    { "category_name", "Personal" },
                    { "category_id", 9 }
                };
                categorylist.Add(categorydata);

                resStruct.Add("classified_categories", categorylist);
            }

            if(loginData.InventorySkeleton != null)
            {
                var folderArray = new AnArray();
                foreach(InventoryFolder folder in loginData.InventorySkeleton)
                {
                    var folderData = new Map
                    {
                        { "folder_id", folder.ID },
                        { "parent_id", folder.ParentFolderID },
                        { "name", folder.Name },
                        { "type_default", (int)folder.InventoryType },
                        { "version", folder.Version }
                    };
                    folderArray.Add(folderData);
                }
                resStruct.Add("inventory-skeleton", folderArray);
            }

            resStruct.Add("sim_ip", ((IPEndPoint)loginData.DestinationInfo.SimIP).Address.ToString());
            resStruct.Add("map-server-url", loginData.CircuitInfo.MapServerURL);

            if(loginData.Friends != null)
            {
                var friendsArray = new AnArray();
                foreach(FriendInfo fi in loginData.Friends)
                {
                    var friendData = new Map
                    {
                        { "buddy_id", fi.Friend.ID },
                        { "buddy_rights_given", (int)fi.UserGivenFlags },
                        { "buddy_rights_has", (int)fi.FriendGivenFlags }
                    };
                    friendsArray.Add(friendData);
                }
                resStruct.Add("buddy-list", friendsArray);
            }

            if(loginData.ActiveGestures != null)
            {
                var gestureArray = new AnArray();
                foreach(InventoryItem item in loginData.ActiveGestures)
                {
                    var gestureData = new Map
                    {
                        ["asset_id"] = item.AssetID,
                        ["item_id"] = item.ID
                    };
                    gestureArray.Add(gestureData);
                }
                resStruct.Add("gestures", gestureArray);
            }

            resStruct.Add("http_port", loginData.DestinationInfo.ServerHttpPort);
            resStruct.Add("sim_port", loginData.DestinationInfo.ServerPort);
            resStruct.Add("start_location", loginData.DestinationInfo.StartLocation);

            if(loginData.HaveGridLibrary && loginData.LoginOptions.Contains(Option_InventoryLibOwner))
            {
                var data = new Map
                {
                    ["agent_id"] = m_GridLibraryOwner
                };
                var ar = new AnArray
                {
                    data
                };
                resStruct.Add("inventory-lib-owner", ar);
            }

            var initial_outfit_data = new Map
            {
                { "folder_name", "Nightclub Female" },
                { "gender", "female" }
            };
            var initial_outfit_array = new AnArray
            {
                initial_outfit_data
            };
            resStruct.Add("initial-outfit", initial_outfit_array);

            if(loginData.InventoryLibSkeleton != null)
            {
                var folderArray = new AnArray();
                foreach(InventoryFolder folder in loginData.InventoryLibSkeleton)
                {
                    var folderData = new Map
                    {
                        { "folder_id", folder.ID },
                        { "parent_id", folder.ParentFolderID },
                        { "name", folder.Name },
                        { "type_default", (int)folder.InventoryType },
                        { "version", folder.Version }
                    };
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

            var globalTextureData = new Map
            {
                { "cloud_texture_id", "dc4b9f0b-d008-45c6-96a4-01dd947ac621" },
                { "sun_texture_id", "cce0f112-878f-4586-a2e2-a8f104bba271" },
                { "moon_texture_id", "ec4b9f0b-d008-45c6-96a4-01dd947ac621" }
            };
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

        private const string Option_InventoryRoot = "inventory-root";
        private const string Option_InventorySkeleton = "inventory-skeleton";
        private const string Option_InventoryLibRoot = "inventory-lib-root";
        private const string Option_InventoryLibOwner = "inventory-lib-owner";
        private const string Option_inventoryLibSkeleton = "inventory-skel-lib";
        private const string Option_Gestures = "gestures";
        private const string Option_EventCategories = "event_categories";
        private const string Option_EventNotifications = "event_notifications";
        private const string Option_ClassifiedCategories = "classified_categories";
        private const string Option_BuddyList = "buddy-list";
        private const string Option_UiConfig = "ui-config";
        private const string Option_LoginFlags = "login-flags";
        private const string Option_GlobalTextures = "global-textures";
        private const string Option_AdultCompliant = "adult_compliant";

        private readonly string[] RequiredParameters = new string[] { "first", "last", "start", "passwd", "channel", "version", "mac", "id0" };

        [Serializable]
        private class LoginFailResponseException : Exception
        {
            public string Reason { get; }

            public LoginFailResponseException(string reason, string message)
                : base(message)
            {
                Reason = reason;
            }
        }

        private XmlRpc.XmlRpcResponse LoginFailResponse(string reason, string message)
        {
            var m = new Map
            {
                { "reason", reason },
                { "message", message },
                { "login", false }
            };
            return new XmlRpc.XmlRpcResponse()
            {
                ReturnValue = m
            };
        }

        public void HandleLoginRedirect(HttpRequest httpreq)
        {
            using (var httpres = httpreq.BeginResponse(HttpStatusCode.RedirectKeepVerb, "Permanently moved"))
            {
                httpres.Headers.Add("Location", m_HttpsServer.ServerURI);
            }
        }

        #region Server Parameters
        private readonly object m_ConfigUpdateLock = new object();

        private const string ConfigIssueText = "Server parameter \"AllowLoginViaHttpWhenHttpsIsConfigured\" is set to true. Please disable it.";
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
                m_AllowLoginViaHttpWhenHttpsIsConfigured = bool.TryParse(value, out val) && val;
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
            m_MaxAgentGroups = (!string.IsNullOrEmpty(value) && int.TryParse(value, out val)) ?
                 val : 42;
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
                bool val;
                m_GridLibraryEnabled = !string.IsNullOrEmpty(value) && bool.TryParse(value, out val) && val;
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
            m_AllowMultiplePresences = bool.TryParse(value, out val) && val;
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
}
