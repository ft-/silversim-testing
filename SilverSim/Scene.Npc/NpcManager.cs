// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Agent;
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
        private static readonly ILog m_Log = LogManager.GetLogger("NPC MANAGER");

        string m_PersistentInventoryServiceName;
        string m_NonpersistentInventoryServiceName;
        string m_PersistentProfileServiceName;
        string m_NonpersistentProfileServiceName;
        string m_PresenceServiceName;
        InventoryServiceInterface m_PersistentInventoryService;
        InventoryServiceInterface m_NonpersistentInventoryService;
        ProfileServiceInterface m_PersistentProfileService;
        ProfileServiceInterface m_NonpersistentProfileService;
        PresenceServiceInterface m_PresenceService;
        IAdminWebIF m_AdminWebIF;

        public NpcManager(IConfig ownConfig)
        {
            m_PersistentProfileServiceName = ownConfig.GetString("PersistentProfileService", string.Empty);
            m_NonpersistentProfileServiceName = ownConfig.GetString("NonpersistentProfileService", string.Empty);
            m_PersistentInventoryServiceName = ownConfig.GetString("PersistentInventoryService", string.Empty);
            m_NonpersistentInventoryServiceName = ownConfig.GetString("NonpersistentInventoryService", string.Empty);
            m_PresenceServiceName = ownConfig.GetString("PresenceService", string.Empty);
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

            }
            /* non persistent inventory is needed for baking logic */
            m_NonpersistentInventoryService = loader.GetService<InventoryServiceInterface>(m_NonpersistentInventoryServiceName);
            m_NonpersistentAgentServices.Add(m_NonpersistentInventoryService);

            /* presence is optional */
            if (!string.IsNullOrEmpty(m_PresenceServiceName) || !string.IsNullOrEmpty(m_PersistentInventoryServiceName))
            {
                m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
                m_PersistentAgentServices.Add(m_PresenceService);
                m_PersistentInventoryService = loader.GetService<InventoryServiceInterface>(m_PersistentInventoryServiceName);
                m_PersistentAgentServices.Add(m_PersistentInventoryService);
            }
            if(!string.IsNullOrEmpty(m_PersistentProfileServiceName))
            {
                m_PersistentProfileService = loader.GetService<ProfileServiceInterface>(m_PersistentProfileServiceName);
                m_PersistentAgentServices.Add(m_PersistentProfileService);
            }
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
            if(null != m_PresenceService)
            {
                foreach(PresenceInfo npcInfo in m_PresenceService.GetPresencesInRegion(scene.ID))
                {
                    NpcAgent agent;
                    try
                    {
                        agent = new NpcAgent(npcInfo.UserID.ID, npcInfo.UserID.FirstName, npcInfo.UserID.LastName, null, m_PersistentAgentServices);
                        /* npcowner and group is not yet persisted */
                        scene.Add(agent);
                    }
                    catch
                    {
                        m_Log.WarnFormat("Failed to instantiate persistent NPC {0} {1} ({2})", npcInfo.UserID.FirstName, npcInfo.UserID.LastName, npcInfo.UserID.ID.ToString());
                        continue;
                    }

                    try
                    {
                        agent.RebakeAppearance();
                    }
                    catch
                    {
                        m_Log.WarnFormat("Failed to rebake persistent NPC {0} {1} ({2})", npcInfo.UserID.FirstName, npcInfo.UserID.LastName, npcInfo.UserID.ID.ToString());
                    }
                }
            }
        }

        void OnSceneRemoved(SceneInterface scene)
        {
            m_KnownScenes.Remove(scene.ID);
            List<UUID> removeList = new List<UUID>();
            foreach (NpcAgent agent in m_NpcAgents.Values)
            {
                if (agent.CurrentScene == scene)
                {
                    removeList.Add(agent.ID);
                }
            }

            foreach (UUID id in removeList)
            {
                m_NonpersistentInventoryService.Remove(UUID.Zero, id);
                if (null != m_NonpersistentProfileService)
                {
                    m_NonpersistentProfileService.Remove(UUID.Zero, id);
                }
                m_NpcAgents.Remove(id);
            }
        }

        #region Control Functions
        public NpcAgent CreateNpc(UUID sceneid, UUI owner, UGI group, string firstName, string lastName, Vector3 position, Notecard nc, NpcOptions options = NpcOptions.None)
        {
            SceneInterface scene;
            AgentServiceList agentServiceList = m_NonpersistentAgentServices;
            InventoryServiceInterface inventoryService = m_NonpersistentInventoryService;
            if((options & NpcOptions.Persistent) != NpcOptions.None)
            {
                if(m_PresenceService == null)
                {
                    throw new InvalidOperationException("Persistence of NPCs not configured");
                }
                agentServiceList = m_PersistentAgentServices;
                inventoryService = m_PersistentInventoryService;
            }

            UUID npcId = UUID.Random;

            if (m_KnownScenes.TryGetValue(sceneid, out scene))
            {
                NpcAgent agent = new NpcAgent(npcId, firstName, lastName, null, agentServiceList);
                agent.NpcOwner = owner;
                agent.Group = group;
                try
                {
                    m_NpcAgents.Add(agent.ID, agent);
                    PresenceInfo npcInfo = new PresenceInfo();
                    npcInfo.RegionID = sceneid;
                    npcInfo.SecureSessionID = UUID.Random;
                    npcInfo.SessionID = UUID.Random;
                    npcInfo.UserID = agent.Owner;
                    m_PresenceService[npcInfo.SessionID, agent.ID, PresenceServiceInterface.SetType.Login] = npcInfo;
                    scene.Add(agent);
                    return agent;
                }
                catch
                {
                    if(m_PresenceService != null)
                    {
                        m_PresenceService.Remove(UUID.Zero, agent.ID);
                    }
                    inventoryService.Remove(UUID.Zero, agent.ID);
                    m_NpcAgents.Remove(agent.ID);
                    throw;
                }
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
                    sb.AppendFormat("Npc {0} {1} ({2})\n- Owner: {3}", agent.Owner.FirstName, agent.Owner.LastName, agent.Owner.ID.ToString(), agent.NpcOwner.FullName);
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