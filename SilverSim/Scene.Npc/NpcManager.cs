// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SilverSim.Scene.Npc
{
    [Description("NPC Manager")]
    public class NpcManager : IPlugin
    {
        sealed class NpcNonPersistentPresenceService : NpcPresenceServiceInterface
        {
            public NpcNonPersistentPresenceService()
            {

            }

            public override List<NpcPresenceInfo> this[UUID regionID]
            {
                get
                {
                    return new List<NpcPresenceInfo>();
                }
            }

            public override void Remove(UUID scopeID, UUID npcID)
            {
                /* nothing to do */
            }

            public override void Store(NpcPresenceInfo presenceInfo)
            {
                /* nothing to do */
            }
        }

        private static readonly ILog m_Log = LogManager.GetLogger("NPC MANAGER");

        string m_PersistentInventoryServiceName;
        string m_NonpersistentInventoryServiceName;
        string m_PersistentProfileServiceName;
        string m_NonpersistentProfileServiceName;
        string m_NpcPresenceServiceName;
        InventoryServiceInterface m_PersistentInventoryService;
        InventoryServiceInterface m_NonpersistentInventoryService;
        ProfileServiceInterface m_PersistentProfileService;
        ProfileServiceInterface m_NonpersistentProfileService;
        NpcPresenceServiceInterface m_NpcPresenceService;
        IAdminWebIF m_AdminWebIF;

        public NpcManager(IConfig ownConfig)
        {
            m_PersistentProfileServiceName = ownConfig.GetString("PersistentProfileService", string.Empty);
            m_NonpersistentProfileServiceName = ownConfig.GetString("NonpersistentProfileService", string.Empty);
            m_PersistentInventoryServiceName = ownConfig.GetString("PersistentInventoryService", string.Empty);
            m_NonpersistentInventoryServiceName = ownConfig.GetString("NonpersistentInventoryService", string.Empty);
            m_NpcPresenceServiceName = ownConfig.GetString("PresenceService", string.Empty);
        }

        readonly RwLockedDictionary<UUID, NpcAgent> m_NpcAgents = new RwLockedDictionary<UUID, NpcAgent>();
        AgentServiceList m_NonpersistentAgentServices = new AgentServiceList();
        AgentServiceList m_PersistentAgentServices = new AgentServiceList();

        public void Startup(ConfigurationLoader loader)
        {
            List<IAdminWebIF> webifs = loader.GetServicesByValue<IAdminWebIF>();
            if(webifs.Count > 0)
            {
                m_AdminWebIF = webifs[0];
                m_AdminWebIF.JsonMethods.Add("npcs.show", HandleShowNpcs);
                m_AdminWebIF.JsonMethods.Add("npc.remove", HandleRemoveNpc);
                m_AdminWebIF.AutoGrantRights["npcs.manage"].Add("npcs.view");
            }
            /* non persistent inventory is needed for baking logic */
            m_NonpersistentInventoryService = loader.GetService<InventoryServiceInterface>(m_NonpersistentInventoryServiceName);
            m_NonpersistentAgentServices.Add(m_NonpersistentInventoryService);
            m_NonpersistentAgentServices.Add(new NpcNonPersistentPresenceService());

            /* persistence is optional */
            if (!string.IsNullOrEmpty(m_NpcPresenceServiceName) || !string.IsNullOrEmpty(m_PersistentInventoryServiceName))
            {
                m_NpcPresenceService = loader.GetService<NpcPresenceServiceInterface>(m_NpcPresenceServiceName);
                m_PersistentAgentServices.Add(m_NpcPresenceService);
                m_PersistentInventoryService = loader.GetService<InventoryServiceInterface>(m_PersistentInventoryServiceName);
                m_PersistentAgentServices.Add(m_PersistentInventoryService);

                /* profile is optional */
                if (!string.IsNullOrEmpty(m_PersistentProfileServiceName))
                {
                    m_PersistentProfileService = loader.GetService<ProfileServiceInterface>(m_PersistentProfileServiceName);
                    m_PersistentAgentServices.Add(m_PersistentProfileService);
                }
            }

            /* profile is optional */
            if (!string.IsNullOrEmpty(m_NonpersistentProfileServiceName))
            {
                m_NonpersistentProfileService = loader.GetService<ProfileServiceInterface>(m_NonpersistentProfileServiceName);
                m_NonpersistentAgentServices.Add(m_NonpersistentProfileService);
            }
            loader.Scenes.OnRegionAdd += OnSceneAdded;
            loader.Scenes.OnRegionRemove += OnSceneRemoved;

            loader.CommandRegistry.ShowCommands.Add("npcs", ShowNpcsCommand);
            loader.CommandRegistry.RemoveCommands.Add("npc", RemoveNpcCommand);
        }

        readonly RwLockedDictionary<UUID, SceneInterface> m_KnownScenes = new RwLockedDictionary<UUID, SceneInterface>();
        void OnSceneAdded(SceneInterface scene)
        {
            m_KnownScenes.Add(scene.ID, scene);
            if(null != m_NpcPresenceService)
            {
                foreach(NpcPresenceInfo npcInfo in m_NpcPresenceService[scene.ID])
                {
                    NpcAgent agent;
                    try
                    {
                        agent = new NpcAgent(npcInfo.Npc, null, m_PersistentAgentServices);
                        agent.GlobalPosition = npcInfo.Position;
                        agent.LookAt = npcInfo.LookAt;
                        agent.NpcOwner = npcInfo.Owner;
                        agent.Group = npcInfo.Group;
                        scene.Add(agent);
                        try
                        {
                            if (npcInfo.SittingOnObjectID != UUID.Zero)
                            {
                                agent.DoSit(npcInfo.SittingOnObjectID);
                            }
                        }
                        catch
                        {
                            m_Log.WarnFormat("Failed to sit persistent NPC {0} {1} ({2}) on object id {3}", npcInfo.Npc.FirstName, npcInfo.Npc.LastName, npcInfo.Npc.ID.ToString(), npcInfo.SittingOnObjectID.ToString());
                        }
                    }
                    catch
                    {
                        m_Log.WarnFormat("Failed to instantiate persistent NPC {0} {1} ({2})", npcInfo.Npc.FirstName, npcInfo.Npc.LastName, npcInfo.Npc.ID.ToString());
                        continue;
                    }

                    try
                    {
                        agent.RebakeAppearance();
                    }
                    catch
                    {
                        m_Log.WarnFormat("Failed to rebake persistent NPC {0} {1} ({2})", npcInfo.Npc.FirstName, npcInfo.Npc.LastName, npcInfo.Npc.ID.ToString());
                    }
                }
            }
        }

        void OnSceneRemoved(SceneInterface scene)
        {
            m_KnownScenes.Remove(scene.ID);
            Dictionary<UUID, NpcAgent> removeList = new Dictionary<UUID, NpcAgent>();
            foreach (NpcAgent agent in m_NpcAgents.Values)
            {
                if (agent.CurrentScene == scene)
                {
                    removeList.Add(agent.ID, agent);
                }
            }

            foreach (KeyValuePair<UUID, NpcAgent> kvp in removeList)
            {
                /* we have to call the next two removes explicitly. We only want to act upon non-persisted data */
                m_NonpersistentInventoryService.Remove(UUID.Zero, kvp.Key);
                if (null != m_NonpersistentProfileService)
                {
                    m_NonpersistentProfileService.Remove(UUID.Zero, kvp.Key);
                }
                NpcPresenceInfo presenceInfo = new NpcPresenceInfo();
                NpcAgent npc = kvp.Value;
                presenceInfo.Npc = npc.Owner;
                presenceInfo.Owner = npc.NpcOwner;
                presenceInfo.Position = npc.Position;
                presenceInfo.LookAt = npc.LookAt;
                presenceInfo.Group = npc.Group;
                presenceInfo.RegionID = npc.SceneID;
                IObject obj = npc.SittingOnObject;
                presenceInfo.SittingOnObjectID = obj != null ? obj.ID : UUID.Zero;
               
                if(m_NpcAgents.Remove(kvp.Key))
                {
                    /* we do not distinguish persistent/non-persistent here since NpcAgent has a property for referencing it */
                    npc.NpcPresenceService.Store(presenceInfo);
                }
            }
        }

        #region Control Functions
        public NpcAgent CreateNpc(UUID sceneid, UUI owner, UGI group, string firstName, string lastName, Vector3 position, Notecard nc, NpcOptions options = NpcOptions.None)
        {
            SceneInterface scene;
            AgentServiceList agentServiceList = m_NonpersistentAgentServices;
            
            if((options & NpcOptions.Persistent) != NpcOptions.None)
            {
                if(m_NpcPresenceService == null)
                {
                    throw new InvalidOperationException("Persistence of NPCs not configured");
                }
                agentServiceList = m_PersistentAgentServices;
            }

            NpcPresenceServiceInterface presenceService = agentServiceList.Get<NpcPresenceServiceInterface>();
            InventoryServiceInterface inventoryService = agentServiceList.Get<InventoryServiceInterface>();

            UUI npcId = new UUI();
            npcId.ID = UUID.Random;
            npcId.FirstName = firstName;
            npcId.LastName = lastName;

            if (m_KnownScenes.TryGetValue(sceneid, out scene))
            {
                NpcAgent agent = new NpcAgent(npcId, null, agentServiceList);
                agent.NpcOwner = owner;
                agent.Group = group;
                try
                {
                    m_NpcAgents.Add(agent.ID, agent);
                    NpcPresenceInfo npcInfo = new NpcPresenceInfo();
                    npcInfo.RegionID = sceneid;
                    npcInfo.Npc = agent.Owner;
                    npcInfo.Owner = agent.NpcOwner;
                    npcInfo.Group = agent.Group;
                    inventoryService.CheckInventory(npcInfo.Npc.ID);
                    agent.LoadAppearanceFromNotecard(nc);
                    presenceService.Store(npcInfo);
                    scene.Add(agent);
                }
                catch
                {
                    if(m_NpcPresenceService != null)
                    {
                        presenceService.Remove(UUID.Zero, agent.ID);
                    }
                    inventoryService.Remove(UUID.Zero, agent.ID);
                    m_NpcAgents.Remove(agent.ID);
                    throw;
                }

                try
                {
                    agent.RebakeAppearance();
                }
                catch
                {
                    m_Log.WarnFormat("Failed to rebake NPC {0} {1} ({2})", npcId.FirstName, npcId.LastName, npcId.ID.ToString());
                }
                return agent;
            }

            throw new KeyNotFoundException("Scene not found");
        }

        void RemoveNpcData(NpcAgent npc)
        {
            UUID npcId = npc.ID;
            npc.InventoryService.Remove(UUID.Zero, npcId);
            try
            {
                npc.ProfileService.Remove(UUID.Zero, npcId);
            }
            catch
            {
                /* ignore exception here */
            }
            try
            {
                npc.PresenceService.Remove(UUID.Zero, npcId);
            }
            catch
            {
                /* ignore exception here */
            }
        }

        public bool RemoveNpc(UUID npcId)
        {
            NpcAgent npc;
            if (m_NpcAgents.Remove(npcId, out npc))
            {
                npc.CurrentScene.Remove(npc);
                RemoveNpcData(npc);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetNpc(UUID npcId, out NpcAgent agent)
        {
            return m_NpcAgents.TryGetValue(npcId, out agent);
        }
        #endregion

        #region Console commands
        void ShowNpcsCommand(List<string> args, Main.Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("show npcs - Shows all NPCs in region");
                return;
            }
            UUID selectedScene = io.SelectedScene;
            if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }

            SceneInterface scene;
            if (!m_KnownScenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                StringBuilder sb = new StringBuilder("NPCs:\n----------------------------------------------\n");
                foreach (NpcAgent agent in m_NpcAgents.Values)
                {
                    if (agent.CurrentScene != scene)
                    {
                        continue;
                    }
                    sb.AppendFormat("Npc {0} {1} ({2})\n- Owner: {3}\n", agent.Owner.FirstName, agent.Owner.LastName, agent.Owner.ID.ToString(), agent.NpcOwner.FullName);
                    if(m_NpcPresenceService != null && m_NpcPresenceService[agent.Owner.ID].Count != 0)
                    {
                        sb.AppendFormat("- Persistent NPC\n");
                    }
                }
                io.Write(sb.ToString());
            }
        }

        void RemoveNpcCommand(List<string> args, Main.Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID npcId;
            if (args[0] == "help" || args.Count < 3 || !UUID.TryParse(args[2], out npcId))
            {
                io.Write("remove npc <uuid> - Remove NPC");
                return;
            }

            UUID selectedScene = io.SelectedScene;
            if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }

            SceneInterface scene;
            if (!m_KnownScenes.TryGetValue(selectedScene, out scene))
            {
                scene = null;
            }

            NpcAgent npc;
            if (!m_NpcAgents.TryGetValue(npcId, out npc))
            {
                io.Write("Npc does not exist");
            }
            else if (scene != null && npc.CurrentScene != scene)
            {
                io.Write("Npc is not on the region");
            }
            else if (!m_NpcAgents.Remove(npcId, out npc))
            {
                io.Write("Npc does not exist");
            }
            else
            {
                npc.CurrentScene.Remove(npc);
                RemoveNpcData(npc);
                io.Write("Npc removed");
            }
        }
        #endregion

        #region WebIF
        [AdminWebIfRequiredRight("npcs.manage")]
        void HandleRemoveNpc(HttpRequest req, Map jsondata)
        {
            SceneInterface scene = null;
            if (!jsondata.ContainsKey("id"))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
            }
            UUID npcId = jsondata["id"].AsUUID;

            if (jsondata.ContainsKey("regionid"))
            {
                if (!m_KnownScenes.TryGetValue(jsondata["regionid"].AsUUID, out scene))
                {
                    m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                    return;
                }
            }

            NpcAgent npc;
            if (!m_NpcAgents.TryGetValue(npcId, out npc))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else if (scene != null && npc.CurrentScene != scene)
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else if (!m_NpcAgents.Remove(npcId, out npc))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                npc.CurrentScene.Remove(npc);
                RemoveNpcData(npc);
                m_AdminWebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("npcs.show")]
        void HandleShowNpcs(HttpRequest req, Map jsondata)
        {
            SceneInterface scene = null;
            if (jsondata.ContainsKey("regionid"))
            {
                if (!m_KnownScenes.TryGetValue(jsondata["regionid"].AsUUID, out scene))
                {
                    m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                    return;
                }
            }

            AnArray npcs = new AnArray();
            foreach (NpcAgent agent in m_NpcAgents.Values)
            {
                if (agent.CurrentScene != scene)
                {
                    continue;
                }
                UUI uui;
                Map npcid = new Map();
                uui = agent.Owner;
                npcid.Add("firstname", uui.FirstName);
                npcid.Add("lastname", uui.LastName);
                npcid.Add("id", uui.ID);

                Map npcowner = new Map();
                uui = agent.NpcOwner;
                npcowner.Add("fullname", uui.FullName);
                npcowner.Add("firstname", uui.FirstName);
                npcowner.Add("lastname", uui.LastName);
                npcowner.Add("id", uui.ID);
                if (uui.HomeURI != null)
                {
                    npcowner.Add("homeuri", uui.HomeURI);
                }

                Map npcdata = new Map();
                npcdata.Add("uui", npcid);
                npcdata.Add("owner", npcowner);
                npcdata.Add("persistent", (m_NpcPresenceService != null && m_NpcPresenceService[agent.Owner.ID].Count != 0));
                npcs.Add(npcdata);
            }
            Map res = new Map();
            res.Add("npcs", npcs);
            m_AdminWebIF.SuccessResponse(req, res);
        }
        #endregion
    }

    [PluginName("NpcManager")]
    public class NpcManagerFactory : IPluginFactory
    {
        public NpcManagerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new NpcManager(ownSection);
        }
    }
}