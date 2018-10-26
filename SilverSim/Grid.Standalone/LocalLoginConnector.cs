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
using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Teleport;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Authorization;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Grid;
using SilverSim.Types.UserSession;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Core.Teleport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.Serialization;

namespace SilverSim.Grid.Standalone
{
    [Description("Local Standalone Login Connector")]
    [PluginName("LocalLoginHandler")]
    public sealed class LocalLoginConnector : IPlugin, ILoginConnectorServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LOCAL LOGIN HANDLER");
        private GridServiceInterface m_GridService;
        private BaseHttpServer m_HttpServer;

        private readonly string m_LocalUserAccountServiceName;
        private UserAccountServiceInterface m_LocalUserAccountService;
        private readonly string m_LocalInventoryServiceName;
        private InventoryServiceInterface m_LocalInventoryService;
        private readonly string m_LocalAssetServiceName;
        private AssetServiceInterface m_LocalAssetService;
        private readonly string m_LocalProfileServiceName;
        private ProfileServiceInterface m_LocalProfileService;
        private readonly string m_LocalUserSessionServiceName;
        private UserSessionServiceInterface m_LocalUserSessionService;
        private readonly string m_LocalFriendsServiceName;
        private FriendsServiceInterface m_LocalFriendsService;
        private readonly string m_LocalOfflineIMServiceName;
        private OfflineIMServiceInterface m_LocalOfflineIMService;
        private readonly string m_LocalGroupsServiceName;
        private GroupsServiceInterface m_LocalGroupsService;
        private readonly string m_LocalEconomyServiceName;
        private EconomyServiceInterface m_LocalEconomyService;
        public List<ITeleportHandlerFactoryServiceInterface> m_TeleportProtocols = new List<ITeleportHandlerFactoryServiceInterface>();

        private List<AuthorizationServiceInterface> m_AuthorizationServices;
        private List<IProtocolExtender> m_PacketHandlerPlugins = new List<IProtocolExtender>();
        private string m_GatekeeperURI;
        private CapsHttpRedirector m_CapsRedirector;
        private SceneList m_Scenes;
        private CommandRegistry m_CommandRegistry;

        private readonly string m_GridServiceName;

        private CommandRegistry Commands { get; set; }

        [Serializable]
        public class LoginFailedException : Exception
        {
            public LoginFailedException()
            {
            }

            public LoginFailedException(string message)
                : base(message)
            {
            }

            protected LoginFailedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public LoginFailedException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        public LocalLoginConnector(IConfig ownConfig)
        {
            m_LocalUserAccountServiceName = ownConfig.GetString("UserAccountService", "UserAccountService");
            m_GridServiceName = ownConfig.GetString("GridService", "GridService");
            m_LocalInventoryServiceName = ownConfig.GetString("LocalInventoryService", "InventoryService");
            m_LocalAssetServiceName = ownConfig.GetString("LocalAssetService", "AssetService");
            m_LocalProfileServiceName = ownConfig.GetString("LocalProfileService", "ProfileService");
            m_LocalFriendsServiceName = ownConfig.GetString("LocalFriendsService", "FriendsService");
            m_LocalUserSessionServiceName = ownConfig.GetString("LocalUserSessionService", "UserSessionService");
            m_LocalOfflineIMServiceName = ownConfig.GetString("LocalOfflineIMService", "OfflineIMService");
            m_LocalGroupsServiceName = ownConfig.GetString("LocalGroupsService", string.Empty);
            m_LocalEconomyServiceName = ownConfig.GetString("LocalEconomyService", "EconomyService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_CommandRegistry = loader.CommandRegistry;
            m_TeleportProtocols = loader.GetServicesByValue<ITeleportHandlerFactoryServiceInterface>();
            m_Scenes = loader.Scenes;
            m_CapsRedirector = loader.CapsRedirector;
            m_AuthorizationServices = loader.GetServicesByValue<AuthorizationServiceInterface>();
            m_HttpServer = loader.HttpServer;
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            Commands = loader.CommandRegistry;
            m_PacketHandlerPlugins = loader.GetServicesByValue<IProtocolExtender>();
            m_GatekeeperURI = loader.GatekeeperURI;

            loader.GetService(m_LocalUserAccountServiceName, out m_LocalUserAccountService);
            loader.GetService(m_LocalAssetServiceName, out m_LocalAssetService);
            loader.GetService(m_LocalInventoryServiceName, out m_LocalInventoryService);
            if (!string.IsNullOrEmpty(m_LocalProfileServiceName))
            {
                loader.GetService(m_LocalProfileServiceName, out m_LocalProfileService);
            }
            loader.GetService(m_LocalFriendsServiceName, out m_LocalFriendsService);
            loader.GetService(m_LocalUserSessionServiceName, out m_LocalUserSessionService);
            loader.GetService(m_LocalOfflineIMServiceName, out m_LocalOfflineIMService);
            if (!string.IsNullOrEmpty(m_LocalGroupsServiceName))
            {
                loader.GetService(m_LocalGroupsServiceName, out m_LocalGroupsService);
            }
            if (!loader.TryGetService(m_LocalEconomyServiceName, out m_LocalEconomyService))
            {
                m_LocalEconomyService = null;
            }
        }

        public void LoginTo(UserAccount account, ClientInfo clientInfo, SessionInfo sessionInfo, DestinationInfo destinationInfo, CircuitInfo circuitInfo, AppearanceInfo appearance, TeleportFlags flags, out string seedCapsURI)
        {
            if(!destinationInfo.LocalToGrid)
            {
                throw new NotSupportedException("Remote login not supported");
            }

            string lastMessage = string.Empty;

            if (destinationInfo.ID != UUID.Zero)
            {
                /* try specified destination first */
                destinationInfo.TeleportFlags = flags | TeleportFlags.ViaLogin;
                try
                {
                    QueryAccess(destinationInfo, account, destinationInfo.Position);
                    PostAgent_Local(account, clientInfo, sessionInfo, destinationInfo, circuitInfo, appearance, UUID.Random, (int)WearableType.NumWearables, out seedCapsURI);
                    return;
                }
                catch (Exception e)
                {
                    m_Log.Debug(string.Format("Failed to login {0} {1} to original destination {2} ({3})", account.Principal.FirstName, account.Principal.LastName, destinationInfo.Name, destinationInfo.ID), e);
                    lastMessage = e.Message;
                }
            }

            foreach (RegionInfo fallbackRegion in m_GridService.GetFallbackRegions(account.ScopeID))
            {
                destinationInfo.UpdateFromRegion(fallbackRegion);
                destinationInfo.StartLocation = "safe";
                destinationInfo.Position = new Vector3(128, 128, 23);
                destinationInfo.LookAt = Vector3.UnitX;
                destinationInfo.TeleportFlags = flags | TeleportFlags.ViaRegionID;

                try
                {
                    QueryAccess(destinationInfo, account, destinationInfo.Position);
                    PostAgent_Local(account, clientInfo, sessionInfo, destinationInfo, circuitInfo, appearance, UUID.Random, (int)WearableType.NumWearables, out seedCapsURI);
                    return;
                }
                catch (Exception e)
                {
                    m_Log.Debug(string.Format("Failed to login {0} {1} to fallback destination {2} ({3})", account.Principal.FirstName, account.Principal.LastName, destinationInfo.Name, destinationInfo.ID), e);
                    if (string.IsNullOrEmpty(lastMessage))
                    {
                        lastMessage = e.Message;
                    }
                }
            }
            throw new LoginFailedException("No suitable destination found");
        }

        private void QueryAccess(DestinationInfo dInfo, UserAccount account, Vector3 position)
        {
            foreach (AuthorizationServiceInterface authService in m_AuthorizationServices)
            {
                authService.QueryAccess(account.Principal, dInfo.ID);
            }
        }

        public class StandalonePresenceService : IPresenceServiceInterface
        {
            private readonly UserSessionServiceInterface m_UserSessionService;

            public StandalonePresenceService(UserSessionServiceInterface userSessionService)
            {
                m_UserSessionService = userSessionService;
            }

            public bool Remove(UUID sessionID) =>
                m_UserSessionService.Remove(sessionID);
        }

        private void PostAgent_Local(UserAccount account, ClientInfo clientInfo, SessionInfo sessionInfo, DestinationInfo destinationInfo, CircuitInfo circuitInfo, AppearanceInfo appearance, UUID capsId, int maxAllowedWearables, out string capsPath)
        {
            SceneInterface scene;
            if (!m_Scenes.TryGetValue(destinationInfo.ID, out scene))
            {
                throw new LoginFailedException(string.Format("No destination for agent {0}", account.Principal.FullName));
            }

            /* We have established trust of home grid by verifying its agent. 
             * At least agent and grid belong together.
             * 
             * Now, we can validate the access of the agent.
             */
            var ad = new AuthorizationServiceInterface.AuthorizationData
            {
                ClientInfo = clientInfo,
                SessionInfo = sessionInfo,
                AccountInfo = account,
                DestinationInfo = destinationInfo,
                AppearanceInfo = appearance
            };
            foreach (AuthorizationServiceInterface authService in m_AuthorizationServices)
            {
                authService.Authorize(ad);
            }

            try
            {
                IAgent sceneAgent = scene.Agents[account.Principal.ID];
                if (sceneAgent.Owner.EqualsGrid(account.Principal))
                {
                    if (circuitInfo.IsChild && !sceneAgent.IsInScene(scene))
                    {
                        /* already got an agent here */
                        m_Log.WarnFormat("Failed to create agent due to duplicate agent id. {0} != {1}", sceneAgent.Owner.ToString(), account.Principal.ToString());
                        throw new LoginFailedException("Failed to create agent due to duplicate agent id");
                    }
                    else if (!circuitInfo.IsChild && !sceneAgent.IsInScene(scene))
                    {
                        /* child becomes root */
                        throw new LoginFailedException("Teleport destination not yet implemented");
                    }
                }
                else if (sceneAgent.Owner.ID == account.Principal.ID)
                {
                    /* we got an agent already and no grid match? */
                    m_Log.WarnFormat("Failed to create agent due to duplicate agent id. {0} != {1}", sceneAgent.Owner.ToString(), account.Principal.ToString());
                    throw new LoginFailedException("Failed to create agent due to duplicate agent id");
                }
            }
            catch
            {
                /* no action needed */
            }

            GridServiceInterface gridService = scene.GridService;

            var serviceList = new AgentServiceList
            {
                m_LocalAssetService,
                m_LocalInventoryService
            };
            if (m_LocalGroupsService != null)
            {
                serviceList.Add(m_LocalGroupsService);
            }
            if (m_LocalProfileService != null)
            {
                serviceList.Add(m_LocalProfileService);
            }
            serviceList.Add(m_LocalFriendsService);
            serviceList.Add(new StandalonePresenceService(m_LocalUserSessionService));
            serviceList.Add(gridService);
            serviceList.Add(m_LocalOfflineIMService);
            if (m_LocalEconomyService != null)
            {
                serviceList.Add(m_LocalEconomyService);
            }
            foreach(ITeleportHandlerFactoryServiceInterface factory in m_TeleportProtocols)
            {
                serviceList.Add(factory.Instantiate(m_CommandRegistry, m_CapsRedirector, m_PacketHandlerPlugins, m_Scenes));
            }

            var agent = new ViewerAgent(
                m_Scenes,
                account.Principal.ID,
                account.Principal.FirstName,
                account.Principal.LastName,
                account.Principal.HomeURI,
                sessionInfo.SessionID,
                sessionInfo.SecureSessionID,
                clientInfo,
                account,
                serviceList)
            {
                ServiceURLs = account.ServiceURLs,

                Appearance = appearance
            };
            try
            {
                scene.DetermineInitialAgentLocation(agent, destinationInfo.TeleportFlags, destinationInfo.Location, destinationInfo.LookAt);
            }
            catch (Exception e)
            {
                m_Log.InfoFormat("Failed to determine initial location for agent {0}: {1}: {2}", account.Principal.FullName, e.GetType().FullName, e.Message);
#if DEBUG
                m_Log.Debug("Exception", e);
#endif
                throw new LoginFailedException(e.Message);
            }

            var udpServer = (UDPCircuitsManager)scene.UDPServer;

            IPAddress ipAddr;
            if (!IPAddress.TryParse(clientInfo.ClientIP, out ipAddr))
            {
                m_Log.InfoFormat("Invalid IP address for agent {0}", account.Principal.FullName);
                throw new LoginFailedException("Invalid IP address");
            }
            var ep = new IPEndPoint(ipAddr, 0);
            var circuit = new AgentCircuit(
                Commands,
                agent,
                udpServer,
                circuitInfo.CircuitCode,
                m_CapsRedirector,
                circuitInfo.CapsPath,
                agent.ServiceURLs,
                m_GatekeeperURI,
                m_PacketHandlerPlugins,
                ep)
            {
                LastTeleportFlags = destinationInfo.TeleportFlags,
                Agent = agent,
                AgentID = account.Principal.ID,
                SessionID = sessionInfo.SessionID
            };
            agent.Circuits.Add(circuit.Scene.ID, circuit);

            try
            {
                scene.Add(agent);
                try
                {
                    udpServer.AddCircuit(circuit);
                }
                catch
                {
                    scene.Remove(agent);
                    throw;
                }
            }
            catch (Exception e)
            {
                m_Log.Debug("Failed agent post", e);
                agent.Circuits.Clear();
                throw new LoginFailedException(e.Message);
            }

            try
            {
                agent.EconomyService?.Login(destinationInfo.ID, account.Principal, sessionInfo.SessionID, sessionInfo.SecureSessionID);
            }
            catch (Exception e)
            {
                m_Log.Warn("Could not contact EconomyService", e);
            }

            if (!circuitInfo.IsChild)
            {
                /* make agent a root agent */
                agent.SceneID = scene.ID;
                try
                {
                    m_LocalUserAccountService.SetPosition(account.ScopeID, account.Principal.ID, new UserRegionData
                    {
                        RegionID = scene.ID,
                        Position = agent.GlobalPosition,
                        LookAt = agent.LookAt,
                        GatekeeperURI = new URI(scene.GatekeeperURI)
                    });
                }
                catch (Exception e)
                {
                    m_Log.Warn("Could not contact UserAccountService", e);
                }
            }

            try
            {
                m_LocalUserSessionService[agent.SessionID, KnownUserSessionInfoVariables.LocationRegionID] = scene.ID.ToString();
            }
            catch (Exception e)
            {
                m_Log.Warn("Could not contact PresenceService", e);
            }
            circuit.LogIncomingAgent(m_Log, circuitInfo.IsChild);
            capsPath = m_CapsRedirector.NewCapsURL(circuitInfo.CapsPath);
        }

        /* prevent auto-resolver on this one */
        public string Name => "Local";
        public bool IsProtocolSupported(string url) => false;
        public bool IsProtocolSupported(string url, Dictionary<string, string> cachedheaders) => false;
        public bool TryGetRegion(string url, string regionName, out RegionInfo rInfo)
        {
            rInfo = default(RegionInfo);
            return false;
        }
        public bool TryGetRegion(string url, UUID regionID, out RegionInfo rInfo)
        {
            rInfo = default(RegionInfo);
            return false;
        }
    }
}
