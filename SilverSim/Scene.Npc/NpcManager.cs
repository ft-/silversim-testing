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
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
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

            public override bool ContainsKey(UUID npcid)
            {
                return false;
            }

            public override bool TryGetValue(UUID npcid, out NpcPresenceInfo presence)
            {
                presence = default(NpcPresenceInfo);
                return false;
            }

            public override bool TryGetValue(UUID regionID, string firstname, string lastname, out NpcPresenceInfo info)
            {
                info = default(NpcPresenceInfo);
                return false;
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

        readonly string m_PersistentInventoryServiceName;
        readonly string m_NonpersistentInventoryServiceName;
        readonly string m_PersistentProfileServiceName;
        readonly string m_NonpersistentProfileServiceName;
        readonly string m_NpcPresenceServiceName;
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
        readonly AgentServiceList m_NonpersistentAgentServices = new AgentServiceList();
        readonly AgentServiceList m_PersistentAgentServices = new AgentServiceList();

        public void Startup(ConfigurationLoader loader)
        {
            List<IAdminWebIF> webifs = loader.GetServicesByValue<IAdminWebIF>();
            if(webifs.Count > 0)
            {
                m_AdminWebIF = webifs[0];
                m_AdminWebIF.JsonMethods.Add("npcs.show", HandleShowNpcs);
                m_AdminWebIF.JsonMethods.Add("npc.remove", HandleRemoveNpc);
                m_AdminWebIF.JsonMethods.Add("npc.get", HandleGetNpc);
                m_AdminWebIF.AutoGrantRights["npcs.manage"].Add("npcs.view");
                m_AdminWebIF.ModuleNames.Add("npcs");
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

            loader.CommandRegistry.AddShowCommand("npcs", ShowNpcsCommand);
            loader.CommandRegistry.AddRemoveCommand("npc", RemoveNpcCommand);
        }

        readonly RwLockedDictionary<UUID, SceneInterface> m_KnownScenes = new RwLockedDictionary<UUID, SceneInterface>();
        void OnSceneAdded(SceneInterface scene)
        {
            m_KnownScenes.Add(scene.ID, scene);
            scene.LoginControl.OnLoginsEnabled += LoginControl_OnLoginsEnabled;
        }

        private void LoginControl_OnLoginsEnabled(UUID sceneID, bool obj)
        {
            SceneInterface scene;
            bool notrezzedbefore = true;
            if(null != m_NpcPresenceService && m_KnownScenes.TryGetValue(sceneID, out scene))
            {
                foreach(NpcPresenceInfo npcInfo in m_NpcPresenceService[scene.ID])
                {
                    NpcAgent agent;
                    IAgent d;
                    if (!scene.Agents.TryGetValue(npcInfo.Npc.ID, out d))
                    {
                        if(notrezzedbefore)
                        {
                            m_Log.Info("Rezzing persistent NPCs");
                            notrezzedbefore = false;
                        }
                        /* only rez if not rezzed before */
                        m_Log.InfoFormat("Rezzing persistent NPC {0} {1} ({2})", npcInfo.Npc.FirstName, npcInfo.Npc.LastName, npcInfo.Npc.ID);
                        try
                        {
                            agent = new NpcAgent(npcInfo.Npc, null, m_PersistentAgentServices);
                            agent.GlobalPosition = npcInfo.Position;
                            agent.LookAt = npcInfo.LookAt;
                            agent.NpcOwner = npcInfo.Owner;
                            agent.Group = npcInfo.Group;
                            agent.CurrentScene = scene;
                            scene.Add(agent);
                            agent.EnableListen();
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
        }

        void OnSceneRemoved(SceneInterface scene)
        {
            scene.LoginControl.OnLoginsEnabled -= LoginControl_OnLoginsEnabled;
            m_KnownScenes.Remove(scene.ID);
            Dictionary<UUID, NpcAgent> removeList = new Dictionary<UUID, NpcAgent>();
            foreach (NpcAgent agent in m_NpcAgents.Values)
            {
                if (agent.CurrentScene == scene)
                {
                    try
                    {
                        agent.DetachAllAttachments();
                    }
                    catch
                    {
                        m_Log.WarnFormat("Failed to detach attachments of NPC {0} {1} ({2})", agent.Owner.FirstName, agent.Owner.LastName, agent.Owner.ID);
                    }
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
                kvp.Value.DisableListen();
               
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
                    presenceService.Store(npcInfo);
                    agent.CurrentScene = scene;
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
                agent.EnableListen();

                try
                {
                    agent.LoadAppearanceFromNotecard(nc);
                }
                catch
                {
                    m_Log.WarnFormat("Failed to load NPC appearance {0} {1} ({2})", npcId.FirstName, npcId.LastName, npcId.ID.ToString());
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
                npc.DisableListen();
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

        public void UnlistenAsNpc(UUID sceneid, UUID objectid, UUID itemid)
        {
            foreach(NpcAgent agent in m_NpcAgents.Values)
            {
                if(agent.SceneID == sceneid)
                {
                    agent.UnlistenAsNpc(objectid, itemid);
                }
            }
        }

        public void UnlistenIM(UUID sceneid, UUID objectid, UUID itemid)
        {
            foreach (NpcAgent agent in m_NpcAgents.Values)
            {
                if (agent.SceneID == sceneid)
                {
                    agent.UnlistenIM(objectid, itemid);
                }
            }
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
                io.Write("Npc is not in the region");
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

            if (jsondata.ContainsKey("regionid") &&
                !m_KnownScenes.TryGetValue(jsondata["regionid"].AsUUID, out scene))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
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

        [AdminWebIfRequiredRight("npcs.view")]
        void HandleGetNpc(HttpRequest req,Map jsondata)
        {
            if (!jsondata.ContainsKey("npcid"))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            NpcAgent agent;
            if(!m_NpcAgents.TryGetValue(jsondata["npcid"].AsUUID, out agent))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
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
            m_AdminWebIF.SuccessResponse(req, npcdata);
        }

        [AdminWebIfRequiredRight("npcs.view")]
        void HandleShowNpcs(HttpRequest req, Map jsondata)
        {
            SceneInterface scene = null;
            if (!jsondata.ContainsKey("regionid") ||
                !m_KnownScenes.TryGetValue(jsondata["regionid"].AsUUID, out scene))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
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