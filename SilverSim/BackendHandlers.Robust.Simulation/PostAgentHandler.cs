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

using log4net;
using Nini.Config;
using SilverSim.BackendConnectors.Robust.Asset;
using SilverSim.BackendConnectors.Robust.GridUser;
using SilverSim.BackendConnectors.Robust.Inventory;
using SilverSim.BackendConnectors.Robust.Presence;
using SilverSim.BackendConnectors.Robust.UserAgent;
using SilverSim.LL.Core;
using SilverSim.LL.Messages.Agent;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.StructuredData.Agent;
using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using ThreadedClasses;

namespace SilverSim.BackendHandlers.Robust.Simulation
{
    #region Service Implementation
    public class PostAgentHandler : IPlugin, IPluginShutdown
    {
        protected static readonly ILog m_Log = LogManager.GetLogger("ROBUST AGENT HANDLER");
        private BaseHttpServer m_HttpServer;
        protected ServerParamServiceInterface m_ServerParams;
        private Main.Common.Caps.CapsHttpRedirector m_CapsRedirector;
        private string m_DefaultGridUserServerURI = string.Empty;
        private string m_DefaultPresenceServerURI = string.Empty;
        private Dictionary<string, IAssetServicePlugin> m_AssetServicePlugins = new Dictionary<string,IAssetServicePlugin>();
        private Dictionary<string, IInventoryServicePlugin> m_InventoryServicePlugins = new Dictionary<string,IInventoryServicePlugin>();
        private List<IPacketHandlerExtender> m_PacketHandlerPlugins = new List<IPacketHandlerExtender>();

        private class GridParameterMap : ICloneable
        {
            public string HomeURI;
            public string AssetServerURI;
            public string InventoryServerURI;
            public string GridUserServerURI = string.Empty;
            public string PresenceServerURI = string.Empty;
            public string AvatarServerURI = string.Empty;
            public string FriendsServerURI = string.Empty;
            public string GatekeeperURI = string.Empty;
            public readonly List<UUID> ValidForSims = new List<UUID>(); /* if empty, all sims are valid */

            public GridParameterMap()
            {

            }

            public object Clone()
            {
                GridParameterMap m = new GridParameterMap();
                m.HomeURI = HomeURI;
                m.GatekeeperURI = GatekeeperURI;
                m.AssetServerURI = AssetServerURI;
                m.InventoryServerURI = InventoryServerURI;
                m.GridUserServerURI = GridUserServerURI;
                m.PresenceServerURI = PresenceServerURI;
                m.AvatarServerURI = AvatarServerURI;
                m.FriendsServerURI = FriendsServerURI;
                m.ValidForSims.AddRange(ValidForSims);
                return m;
            }
        }

        private readonly RwLockedList<GridParameterMap> m_GridParameterMap = new RwLockedList<GridParameterMap>();

        private string m_AgentBaseURL = "/agent/";

        public PostAgentHandler(IConfig ownSection)
        {
            m_DefaultGridUserServerURI = ownSection.GetString("DefaultGridUserServerURI", string.Empty);
            m_DefaultPresenceServerURI = ownSection.GetString("DefaultPresenceServerURI", string.Empty);
        }

        protected PostAgentHandler(string agentBaseURL, IConfig ownSection)
        {
            m_AgentBaseURL = agentBaseURL;
        }

        protected string HeloRequester(string uri)
        {
            if (!uri.EndsWith("="))
            {
                uri = uri.TrimEnd('/') + "/helo/";
            }
            else
            {
                /* simian special */
                if(uri.Contains("?"))
                {
                    uri = uri.Substring(0, uri.IndexOf('?'));
                }
                uri = uri.TrimEnd('/') + "/helo/";
            }

            try
            {
                WebRequest req = HttpWebRequest.Create(uri);
                using(WebResponse response = req.GetResponse())
                {
                    if(response.Headers.Get("X-Handlers-Provided") == null)
                    {
                        return "opensim-robust"; /* let us assume Robust API */
                    }
                    return response.Headers.Get("X-Handlers-Provided");
                }
            }
            catch
            {
                return "opensim-robust"; /* let us assume Robust API */
            }
        }

        public virtual void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing agent post handler for " + m_AgentBaseURL);
            m_ServerParams = loader.GetServerParamStorage();
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add(m_AgentBaseURL, AgentPostHandler);
            foreach(IAssetServicePlugin plugin in loader.GetServicesByValue<IAssetServicePlugin>())
            {
                m_AssetServicePlugins.Add(plugin.Name, plugin);
            }
            foreach(IInventoryServicePlugin plugin in loader.GetServicesByValue<IInventoryServicePlugin>())
            {
                m_InventoryServicePlugins.Add(plugin.Name, plugin);
            }

            m_PacketHandlerPlugins = loader.GetServicesByValue<IPacketHandlerExtender>();

            foreach(IConfig section in loader.Config.Configs)
            {
                if(section.Name.StartsWith("RobustGrid-"))
                {
                    if(!section.Contains("HomeURI") || !section.Contains("AssetServerURI") || !section.Contains("InventoryServerURI"))
                    {
                        m_Log.WarnFormat("Skipping section {0} for missing entries (HomeURI, AssetServerURI and InventoryServerURI are required)", section.Name);
                        continue;
                    }
                    GridParameterMap map = new GridParameterMap();
                    map.HomeURI = section.GetString("HomeURI");
                    map.AssetServerURI = section.GetString("AssetServerURI");
                    map.GridUserServerURI = section.GetString("GridUserServerURI", m_DefaultGridUserServerURI);
                    map.PresenceServerURI = section.GetString("PresenceServerURI", string.Empty);
                    map.AvatarServerURI = section.GetString("AvatarServerURI", string.Empty);
                    map.InventoryServerURI = section.GetString("InventoryServerURI");

                    if (!Uri.IsWellFormedUriString(map.HomeURI, UriKind.Absolute))
                    {
                        m_Log.WarnFormat("Skipping section {0} for invalid URI in HomeURI {1}", section.Name, map.HomeURI);
                        continue;
                    }
                    else if (map.GridUserServerURI != "" && !Uri.IsWellFormedUriString(map.GridUserServerURI, UriKind.Absolute))
                    {
                        m_Log.WarnFormat("Skipping section {0} for invalid URI in GridUserServerURI {1}", section.Name, map.GridUserServerURI);
                        continue;
                    }
                    else if (map.GatekeeperURI != "" && !Uri.IsWellFormedUriString(map.GatekeeperURI, UriKind.Absolute))
                    {
                        m_Log.WarnFormat("Skipping section {0} for invalid URI in GatekeeperURI {1}", section.Name, map.GatekeeperURI);
                        continue;
                    }
                    else if (map.PresenceServerURI != "" && !Uri.IsWellFormedUriString(map.PresenceServerURI, UriKind.Absolute))
                    {
                        m_Log.WarnFormat("Skipping section {0} for invalid URI in PresenceServerURI {1}", section.Name, map.PresenceServerURI);
                        continue;
                    }
                    else if (map.AvatarServerURI != "" && !Uri.IsWellFormedUriString(map.AvatarServerURI, UriKind.Absolute))
                    {
                        m_Log.WarnFormat("Skipping section {0} for invalid URI in AvatarServerURI {1}", section.Name, map.AvatarServerURI);
                        continue;
                    }
                    else if (!Uri.IsWellFormedUriString(map.AssetServerURI, UriKind.Absolute))
                    {
                        m_Log.WarnFormat("Skipping section {0} for invalid URI in AssetServerURI {1}", section.Name, map.AssetServerURI);
                        continue;
                    }
                    else if (!Uri.IsWellFormedUriString(map.InventoryServerURI, UriKind.Absolute))
                    {
                        m_Log.WarnFormat("Skipping section {0} for invalid URI in InventoryServerURI {1}", section.Name, map.InventoryServerURI);
                        continue;
                    }

                    if(section.Contains("ValidFor"))
                    {
                        string[] sims = section.GetString("ValidFor", "").Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                        if(sims.Length != 0)
                        {
                            foreach(string sim in sims)
                            {
                                UUID id;
                                try
                                {
                                    id = UUID.Parse(sim.Trim());
                                }
                                catch
                                {
                                    m_Log.ErrorFormat("Invalid UUID {0} encountered within ValidFor in section {1}", sim, section.Name);
                                    continue;
                                }
                                map.ValidForSims.Add(id);
                            }
                            if(map.ValidForSims.Count == 0)
                            {
                                m_Log.WarnFormat("Grid Parameter Map section {0} will be valid for all sims", section.Name);
                            }
                        }
                    }
                    m_GridParameterMap.Add(map);
                    foreach(string key in section.GetKeys())
                    {
                        if(key.StartsWith("Alias-"))
                        {
                            GridParameterMap map2 = (GridParameterMap)map.Clone();
                            map2.HomeURI = section.GetString(key);
                            m_GridParameterMap.Add(map2);
                        }
                    }
                }
            }

            m_CapsRedirector = loader.GetService<Main.Common.Caps.CapsHttpRedirector>("CapsRedirector");
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        public virtual void Shutdown()
        {
            m_HttpServer.StartsWithUriHandlers.Remove(m_AgentBaseURL);
        }

        private void GetAgentParams(string uri, out UUID agentID, out UUID regionID, out string action)
        {
            agentID = UUID.Zero;
            regionID = UUID.Zero;
            action = "";

            uri = uri.Trim(new char[] { '/' });
            string[] parts = uri.Split('/');
            if(parts.Length < 2)
            {
                throw new InvalidDataException();
            }
            else
            {
                agentID = UUID.Parse(parts[1]);
                if(parts.Length > 2)
                {
                    regionID = UUID.Parse(parts[2]);
                }
                if(parts.Length > 3)
                {
                    action = parts[3];
                }
            }
        }

        GridParameterMap FindGridParameterMap(string homeURI, SceneInterface scene)
        {
            foreach (GridParameterMap map in m_GridParameterMap)
            {
                /* some grids have weird ideas about how to map their URIs, so we need this checking for whether the grid name starts with it */
                if (map.HomeURI.StartsWith(homeURI))
                {
                    if (map.ValidForSims.Count != 0)
                    {
                        if (!map.ValidForSims.Contains(scene.ID))
                        {
                            continue;
                        }
                    }
                    return map;
                }
            }
            return null;
        }

        public void DoAgentResponse(HttpRequest req, string reason, bool success)
        {
            string success_str = success ? "true" : "false";
            string caller = req.CallerIP.ToString();
            string fmt = "{" + string.Format("\"reason\":\"{0}\",\"success\":{1},\"your_ip\":\"{2}\"",
                reason,
                success_str,
                caller) + "}";
            HttpResponse res = req.BeginResponse();
            res.ContentType = "application/json";
            Stream o = res.GetOutputStream();
            byte[] b = Encoding.UTF8.GetBytes(fmt);
            o.Write(b, 0, b.Length);
            res.Close();
        }

        protected virtual void CheckScenePerms(UUID sceneID)
        {

        }

        public void AgentPostHandler(HttpRequest req)
        {
            UUID agentID;
            UUID regionID;
            string action;
            try
            {
                GetAgentParams(req.RawUrl, out agentID, out regionID, out action);
            }
            catch(Exception e)
            {
                m_Log.InfoFormat("Invalid parameters for agent message {0}", req.RawUrl);
                HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, e.Message);
                res.Close();
                return;
            }
            if (req.Method == "POST")
            {
                Stream httpBody = req.Body;
                HttpResponse res;
                if(req.ContentType == "application/x-gzip")
                {
                    httpBody = new GZipStream(httpBody, CompressionMode.Decompress);
                }
                else if(req.ContentType == "application/json")
                {

                }
                else
                {
                    m_Log.InfoFormat("Invalid content for agent message {0}: {1}", req.RawUrl, req.ContentType);
                    res = req.BeginResponse(HttpStatusCode.BadRequest, "Invalid content for agent message");
                    res.Close();
                    return;
                }

                PostData agentPost;
                try
                {
                     agentPost = PostData.Deserialize(httpBody);
                }
                catch(Exception e)
                {
                    m_Log.InfoFormat("Deserialization error for agent message {0}: {1}: {2}\n{3}", req.RawUrl, e.GetType().FullName, e.Message, e.StackTrace.ToString());
                    res = req.BeginResponse(HttpStatusCode.BadRequest, e.Message);
                    res.Close();
                    return;
                }

                SceneInterface scene;
                if(!Scene.Management.Scene.SceneManager.Scenes.TryGetValue(agentPost.Destination.ID, out scene))
                {
                    m_Log.InfoFormat("No destination for agent {0}", req.RawUrl);
                    res = req.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                    res.Close();
                    return;
                }

                try
                {
                    CheckScenePerms(agentPost.Destination.ID);
                }
                catch
                {
                    m_Log.InfoFormat("No destination for agent {0}", req.RawUrl);
                    res = req.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                    res.Close();
                    return;
                }

                string assetServerURI = agentPost.Account.ServiceURLs["AssetServerURI"];
                string inventoryServerURI = agentPost.Account.ServiceURLs["InventoryServerURI"];
                string gatekeeperURI = scene.GatekeeperURI;

                ProfileServiceInterface profileService = null;
                UserAgentServiceInterface userAgentService = null;
                PresenceServiceInterface presenceService = null;
                GridUserServiceInterface gridUserService = null;
                FriendsServiceInterface friendsService = null;

                GridParameterMap gridparams = FindGridParameterMap(agentPost.Account.Principal.HomeURI.ToString(), scene);
                if(gridparams != null)
                {
                    assetServerURI = gridparams.AssetServerURI;
                    inventoryServerURI = gridparams.InventoryServerURI;
                    if (!string.IsNullOrEmpty(gridparams.GatekeeperURI))
                    {
                        gatekeeperURI = gridparams.GatekeeperURI;
                    }
                    if(!string.IsNullOrEmpty(gridparams.GridUserServerURI))
                    {
                        gridUserService = new RobustGridUserConnector(gridparams.GridUserServerURI);
                    }
                    if(!string.IsNullOrEmpty(gridparams.PresenceServerURI))
                    {
                        presenceService = new RobustPresenceConnector(gridparams.PresenceServerURI, agentPost.Account.Principal.HomeURI.ToString());
                    }
                }
                else if(string.IsNullOrEmpty(m_DefaultPresenceServerURI))
                {
                    presenceService = new RobustHGOnlyPresenceConnector(agentPost.Account.Principal.HomeURI.ToString());
                }
                else
                {
                    presenceService = new RobustHGPresenceConnector(m_DefaultPresenceServerURI, agentPost.Account.Principal.HomeURI.ToString());
                }

                UserAgentServiceInterface userAccountConnector = new RobustUserAgentConnector(agentPost.Account.ServiceURLs["HomeURI"]);
                try
                {
                    userAccountConnector.VerifyAgent(agentPost.Session.SessionID, agentPost.Session.ServiceSessionID);
                }
                catch
                {
                    DoAgentResponse(req, "Failed to verify agent at Home Grid", false);
                    return;
                }

                try
                {
                    userAccountConnector.VerifyClient(agentPost.Session.SessionID, agentPost.Client.ClientIP);
                }
                catch
                {
                    DoAgentResponse(req, "Failed to verify client at Home Grid", false);
                    return;
                }

                GroupsServiceInterface groupsService = null;
                AssetServiceInterface assetService;
                InventoryServiceInterface inventoryService;
                string inventoryType = HeloRequester(inventoryServerURI);
                string assetType = HeloRequester(assetServerURI);
                if (string.IsNullOrEmpty(assetType) || assetType == "opensim-robust")
                {
                    assetService = new RobustAssetConnector(assetServerURI);
                }
                else
                {
                    assetService = m_AssetServicePlugins[assetType].Instantiate(assetServerURI);
                }
                if (string.IsNullOrEmpty(inventoryType) || inventoryType == "opensim-robust")
                {
                    inventoryService = new RobustInventoryConnector(inventoryServerURI, groupsService);
                }
                else
                {
                    inventoryService = m_InventoryServicePlugins[assetType].Instantiate(inventoryServerURI);
                }
                GridServiceInterface gridService = scene.GridService;

                AgentServiceList serviceList = new AgentServiceList();
                serviceList.Add(assetService);
                serviceList.Add(inventoryService);
                serviceList.Add(groupsService);
                serviceList.Add(profileService);
                serviceList.Add(friendsService);
                serviceList.Add(userAgentService);
                serviceList.Add(presenceService);
                serviceList.Add(gridUserService);
                serviceList.Add(gridService);

                LLAgent agent = new LLAgent(
                    agentPost.Account.Principal.ID,
                    agentPost.Account.Principal.FirstName,
                    agentPost.Account.Principal.LastName,
                    agentPost.Account.Principal.HomeURI,
                    agentPost.Session.SessionID,
                    agentPost.Session.SecureSessionID,
                    serviceList);
                agent.ServiceURLs = agentPost.Account.ServiceURLs;

                agent.TeleportFlags = agentPost.Destination.TeleportFlags;
                agent.Appearance = agentPost.Appearance;
                agent.GlobalPosition = new Vector3(128, 128, 23);

                LLUDPServer udpServer = (LLUDPServer)scene.UDPServer;

                Circuit circuit = new Circuit(
                    agent, 
                    udpServer, 
                    agentPost.Circuit.CircuitCode, 
                    m_CapsRedirector, 
                    agentPost.Circuit.CapsPath, 
                    agent.ServiceURLs, 
                    gatekeeperURI,
                    m_PacketHandlerPlugins);
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(agentPost.Client.ClientIP), 0);
                circuit.RemoteEndPoint = ep;
                circuit.Agent = agent;
                circuit.AgentID = agentPost.Account.Principal.ID;
                circuit.SessionID = agentPost.Session.SessionID;
                agent.Circuits.Add(circuit.CircuitCode, circuit.Scene.ID, circuit);

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
                    DoAgentResponse(req, e.Message, false);
                    return;
                }
                try
                {
                    gridUserService.SetPosition(agent.Owner, scene.ID, agent.GlobalPosition, agent.LookAt);
                }
                catch(Exception)
                {

                }
                m_Log.InfoFormat("Agent post request {0} {1} (Grid {2}, UUID {3}) TeleportFlags ({4}) Client IP {5} Caps {6} Circuit {7}",
                    agentPost.Account.Principal.FirstName,
                    agentPost.Account.Principal.LastName,
                    agentPost.Account.Principal.HomeURI,
                    agentPost.Account.Principal.ID,
                    agentPost.Destination.TeleportFlags.ToString(),
                    agentPost.Client.ClientIP,
                    agentPost.Circuit.CapsPath,
                    agentPost.Circuit.CircuitCode);
                DoAgentResponse(req, "authorized", true);
            }
            else if(req.Method == "PUT")
            {
                /* this is the rather nasty HTTP variant of the UDP AgentPosition messaging */
                Stream httpBody = req.Body;
                if (req.ContentType == "application/x-gzip")
                {
                    httpBody = new GZipStream(httpBody, CompressionMode.Decompress);
                }
                else if (req.ContentType == "application/json")
                {

                }
                else
                {
                    m_Log.InfoFormat("Invalid content for agent message {0}: {1}", req.RawUrl, req.ContentType);
                    HttpResponse res = req.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Invalid content for agent message");
                    res.Close();
                    return;
                }

                IValue json;

                try
                {
                    json = JSON.Deserialize(httpBody);
                }
                catch (Exception e)
                {
                    m_Log.InfoFormat("Deserialization error for agent message {0}\n{1}", req.RawUrl, e.StackTrace.ToString());
                    HttpResponse res = req.BeginResponse(HttpStatusCode.BadRequest, e.Message);
                    res.Close();
                    return;
                }

                Map param = (Map)json;
                string msgType = param["messageType"].ToString();
                if(msgType == "AgentData")
                {
                    ChildAgentUpdate childAgentData = new ChildAgentUpdate();

                    childAgentData.RegionID = param["region_id"].AsUUID;
                    childAgentData.ViewerCircuitCode = param["circuit_code"].AsUInt;
                    childAgentData.AgentID = param["agent_uuid"].AsUUID;
                    childAgentData.SessionID = param["session_uuid"].AsUUID;
                    childAgentData.AgentPosition = param["position"].AsVector3;
                    childAgentData.AgentVelocity = param["velocity"].AsVector3;
                    childAgentData.Center = param["center"].AsVector3;
                    childAgentData.Size = param["size"].AsVector3;
                    childAgentData.AtAxis = param["at_axis"].AsVector3;
                    childAgentData.LeftAxis = param["left_axis"].AsVector3;
                    childAgentData.UpAxis = param["up_axis"].AsVector3;
                    /*


            if (args.ContainsKey("wait_for_root") && args["wait_for_root"] != null)
                SenderWantsToWaitForRoot = args["wait_for_root"].AsBoolean();
                     */

                    childAgentData.Far = param["far"].AsReal;
                    childAgentData.Aspect = param["aspect"].AsReal;
                    //childAgentData.Throttles = param["throttles"];
                    childAgentData.LocomotionState = param["locomotion_state"].AsUInt;
                    childAgentData.HeadRotation = param["head_rotation"].AsQuaternion;
                    childAgentData.BodyRotation = param["body_rotation"].AsQuaternion;
                    childAgentData.ControlFlags = (ControlFlags)param["control_flags"].AsUInt;
                    childAgentData.EnergyLevel = param["energy_level"].AsReal;
                    childAgentData.GodLevel = (byte)param["god_level"].AsUInt;
                    childAgentData.AlwaysRun = param["always_run"].AsBoolean;
                    childAgentData.PreyAgent = param["prey_agent"].AsUUID;
                    childAgentData.AgentAccess = (byte)param["agent_access"].AsUInt;
                    childAgentData.ActiveGroupID = param["active_group_id"].AsUUID;

                    if(param.ContainsKey("groups") && param["groups"] is AnArray)
                    {
                        AnArray groups = (AnArray)param["groups"];
                        foreach(IValue val in groups)
                        {
                            Map group = (Map)val;
                            ChildAgentUpdate.GroupDataEntry g = new ChildAgentUpdate.GroupDataEntry();
                            g.AcceptNotices = group["accept_notices"].AsBoolean;
                            g.GroupPowers = (GroupPowers)UInt64.Parse(group["group_powers"].ToString());
                            g.GroupID = group["group_id"].AsUUID;
                            childAgentData.GroupData.Add(g);
                        }
                    }

                    if(param.ContainsKey("animations") && param["animations"] is AnArray)
                    {
                        AnArray anims = (AnArray)param["animations"];
                        foreach(IValue val in anims)
                        {
                            Map anim = (Map)val;
                            ChildAgentUpdate.AnimationDataEntry a = new ChildAgentUpdate.AnimationDataEntry();
                            a.Animation = anim["animation"].AsUUID;
                            a.ObjectID = anim["object_id"].AsUUID;
                            childAgentData.AnimationData.Add(a);
                        }
                    }
                    /*

            if (args["default_animation"] != null)
            {
                try
                {
                    DefaultAnim = new Animation((OSDMap)args["default_animation"]);
                }
                catch
                {
                    DefaultAnim = null;
                }
            }

            if (args["animation_state"] != null)
            {
                try
                {
                    AnimState = new Animation((OSDMap)args["animation_state"]);
                }
                catch
                {
                    AnimState = null;
                }
            }
                     * */

                    /*-----------------------------------------------------------------*/
                    /* Appearance */
                    Map appearancePack = (Map)param["packed_appearance"];
                    AppearanceInfo Appearance = new AppearanceInfo();
                    Appearance.AvatarHeight = appearancePack["height"].AsReal;
                    //agentparams.Appearance.Serial = appearancePack["serial"].AsInt;

                    {
                        AnArray vParams = (AnArray)appearancePack["visualparams"];
                        byte[] visualParams = new byte[vParams.Count];

                        int i;
                        for (i = 0; i < vParams.Count; ++i)
                        {
                            visualParams[i] = (byte)vParams[i].AsUInt;
                        }
                        Appearance.VisualParams = visualParams;
                    }

                    {
                        AnArray texArray = (AnArray)appearancePack["textures"];
                        int i;
                        for (i = 0; i < AppearanceInfo.AvatarTextureData.TextureCount; ++i)
                        {
                            Appearance.AvatarTextures[i] = texArray[i].AsUUID;
                        }
                    }

                    {
                        int i;
                        uint n;
                        AnArray wearables = (AnArray)appearancePack["wearables"];
                        for (i = 0; i < (int)WearableType.NumWearables; ++i)
                        {
                            AnArray ar;
                            try
                            {
                                ar = (AnArray)wearables[i];
                            }
                            catch
                            {
                                continue;
                            }
                            n = 0;
                            foreach (IValue val in ar)
                            {
                                Map wp = (Map)val;
                                AgentWearables.WearableInfo wi = new AgentWearables.WearableInfo();
                                wi.ItemID = wp["item"].AsUUID;
                                if (wp.ContainsKey("asset"))
                                {
                                    wi.AssetID = wp["Asset"].AsUUID;
                                }
                                else
                                {
                                    wi.AssetID = UUID.Zero;
                                }
                                WearableType type = (WearableType)i;
                                Appearance.Wearables[type, n++] = wi;
                            }
                        }
                    }

                    {
                        foreach (IValue apv in (AnArray)appearancePack["attachments"])
                        {
                            Map ap = (Map)apv;
                            Appearance.Attachments[(AttachmentPoint)uint.Parse(ap["point"].ToString())][ap["item"].AsUUID] = UUID.Zero;
                        }
                    }

                    /*
            if ((args["controllers"] != null) && (args["controllers"]).Type == OSDType.Array)
            {
                OSDArray controls = (OSDArray)(args["controllers"]);
                Controllers = new ControllerData[controls.Count];
                int i = 0;
                foreach (OSD o in controls)
                {
                    if (o.Type == OSDType.Map)
                    {
                        Controllers[i++] = new ControllerData((OSDMap)o);
                     * 
                        public void UnpackUpdateMessage(OSDMap args)
                        {
                            if (args["object"] != null)
                                ObjectID = args["object"].AsUUID();
                            if (args["item"] != null)
                                ItemID = args["item"].AsUUID();
                            if (args["ignore"] != null)
                                IgnoreControls = (uint)args["ignore"].AsInteger();
                            if (args["event"] != null)
                                EventControls = (uint)args["event"].AsInteger();
                        }
                                     * 
                    }
                }
            }
                     */

                    /*
            if (args["callback_uri"] != null)
                CallbackURI = args["callback_uri"].AsString();
                     * */

                    /*
            // Attachment objects
            if (args["attach_objects"] != null && args["attach_objects"].Type == OSDType.Array)
            {
                OSDArray attObjs = (OSDArray)(args["attach_objects"]);
                AttachmentObjects = new List<ISceneObject>();
                AttachmentObjectStates = new List<string>();
                foreach (OSD o in attObjs)
                {
                    if (o.Type == OSDType.Map)
                    {
                        OSDMap info = (OSDMap)o;
                        ISceneObject so = scene.DeserializeObject(info["sog"].AsString());
                        so.ExtraFromXmlString(info["extra"].AsString());
                        so.HasGroupChanged = info["modified"].AsBoolean();
                        AttachmentObjects.Add(so);
                        AttachmentObjectStates.Add(info["state"].AsString());
                    }
                }
            }

            if (args["parent_part"] != null)
                ParentPart = args["parent_part"].AsUUID();
            if (args["sit_offset"] != null)
                Vector3.TryParse(args["sit_offset"].AsString(), out SitOffset);
                     */

                    SceneInterface scene;
                    if (Scene.Management.Scene.SceneManager.Scenes.TryGetValue(childAgentData.RegionHandle, out scene))
                    {
                        IAgent agent;
                        HttpResponse res;
                        try
                        {
                            agent = scene.Agents[childAgentData.AgentID];
                        }
                        catch
                        {
                            res = req.BeginResponse(HttpStatusCode.BadRequest, "Unknown message type");
                            res.Close();
                            return;
                        }

                        try
                        {
                            agent.HandleMessage(childAgentData);
                            res = req.BeginResponse();
                            res.Close();
                        }
                        catch
                        {
                            res = req.BeginResponse(HttpStatusCode.BadRequest, "Unknown message type");
                            res.Close();
                            return;
                        }
                    }
                    else
                    {
                        HttpResponse res = req.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Unknown message type");
                        res.Close();
                    }
                }
                else if (msgType == "AgentPosition")
                {
                    ChildAgentPositionUpdate childAgentPosition = new ChildAgentPositionUpdate();

                    childAgentPosition.RegionHandle = UInt64.Parse(param["region_handle"].ToString());
                    childAgentPosition.ViewerCircuitCode = param["circuit_code"].AsUInt;
                    childAgentPosition.AgentID = param["agent_uuid"].AsUUID;
                    childAgentPosition.SessionID = param["session_uuid"].AsUUID;
                    childAgentPosition.AgentPosition = param["position"].AsVector3;
                    childAgentPosition.AgentVelocity = param["velocity"].AsVector3;
                    childAgentPosition.Center = param["center"].AsVector3;
                    childAgentPosition.Size = param["size"].AsVector3;
                    childAgentPosition.AtAxis = param["at_axis"].AsVector3;
                    childAgentPosition.LeftAxis = param["left_axis"].AsVector3;
                    childAgentPosition.UpAxis = param["up_axis"].AsVector3;
                    childAgentPosition.ChangedGrid = param["changed_grid"].AsBoolean;
                    /* Far and Throttles are extra in opensim so we have to cope with these on sending */
                    
                    SceneInterface scene;
                    if (Scene.Management.Scene.SceneManager.Scenes.TryGetValue(childAgentPosition.RegionHandle, out scene))
                    {
                        IAgent agent;
                        HttpResponse res;
                        try
                        {
                            agent = scene.Agents[childAgentPosition.AgentID];
                        }
                        catch
                        {
                            res = req.BeginResponse(HttpStatusCode.BadRequest, "Unknown message type");
                            res.Close();
                            return;
                        }

                        try
                        {
                            agent.HandleMessage(childAgentPosition);
                            res = req.BeginResponse();
                            res.Close();
                        }
                        catch
                        {
                            res = req.BeginResponse(HttpStatusCode.BadRequest, "Unknown message type");
                            res.Close();
                            return;
                        }
                    }
                    else
                    {
                        HttpResponse res = req.BeginResponse(HttpStatusCode.BadRequest, "Unknown message type");
                        res.Close();
                    }
                }
                else
                {
                    HttpResponse res = req.BeginResponse(HttpStatusCode.BadRequest, "Unknown message type");
                    res.Close();
                }
            }
            else if(req.Method == "DELETE")
            {
                HttpResponse res = req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                res.Close();
            }
            else if(req.Method == "QUERYACCESS")
            {
                HttpResponse res = req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                res.Close();
            }
            else
            {
                HttpResponse res = req.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                res.Close();
            }
        }
    }
    #endregion

    #region Service Factory
    [PluginName("RobustAgentHandler")]
    public class PostAgentHandlerFactory : IPluginFactory
    {
        public PostAgentHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new PostAgentHandler(ownSection);
        }
    }
    #endregion
}
