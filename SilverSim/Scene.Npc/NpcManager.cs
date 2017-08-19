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
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SilverSim.Scene.Npc
{
    [Description("NPC Manager")]
    [PluginName("NpcManager")]
    public class NpcManager : IPlugin
    {
        private sealed class NpcNonPersistentPresenceService : NpcPresenceServiceInterface
        {
            public override List<NpcPresenceInfo> this[UUID regionID] => new List<NpcPresenceInfo>();

            public override bool ContainsKey(UUID npcid) => false;

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

        private readonly string m_PersistentInventoryServiceName;
        private readonly string m_NonpersistentInventoryServiceName;
        private readonly string m_PersistentProfileServiceName;
        private readonly string m_NonpersistentProfileServiceName;
        private readonly string m_NpcPresenceServiceName;
        private InventoryServiceInterface m_PersistentInventoryService;
        private InventoryServiceInterface m_NonpersistentInventoryService;
        private ProfileServiceInterface m_PersistentProfileService;
        private ProfileServiceInterface m_NonpersistentProfileService;
        private NpcPresenceServiceInterface m_NpcPresenceService;
        private IAdminWebIF m_AdminWebIF;

        public NpcManager(IConfig ownConfig)
        {
            m_PersistentProfileServiceName = ownConfig.GetString("PersistentProfileService", string.Empty);
            m_NonpersistentProfileServiceName = ownConfig.GetString("NonpersistentProfileService", string.Empty);
            m_PersistentInventoryServiceName = ownConfig.GetString("PersistentInventoryService", string.Empty);
            m_NonpersistentInventoryServiceName = ownConfig.GetString("NonpersistentInventoryService", string.Empty);
            m_NpcPresenceServiceName = ownConfig.GetString("PresenceService", string.Empty);
        }

        private readonly RwLockedDictionary<UUID, NpcAgent> m_NpcAgents = new RwLockedDictionary<UUID, NpcAgent>();
        private readonly AgentServiceList m_NonpersistentAgentServices = new AgentServiceList();
        private readonly AgentServiceList m_PersistentAgentServices = new AgentServiceList();

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
            loader.CommandRegistry.AddCreateCommand("npc", CreateNpcCommand);
        }

        private readonly RwLockedDictionary<UUID, SceneInterface> m_KnownScenes = new RwLockedDictionary<UUID, SceneInterface>();
        private void OnSceneAdded(SceneInterface scene)
        {
            m_KnownScenes.Add(scene.ID, scene);
            scene.LoginControl.OnLoginsEnabled += LoginControl_OnLoginsEnabled;
        }

        private void LoginControl_OnLoginsEnabled(UUID sceneID, bool obj)
        {
            SceneInterface scene;
            bool notrezzedbefore = true;
            if(m_NpcPresenceService != null && m_KnownScenes.TryGetValue(sceneID, out scene))
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
                            agent = new NpcAgent(npcInfo.Npc, null, m_PersistentAgentServices)
                            {
                                GlobalPosition = npcInfo.Position,
                                LookAt = npcInfo.LookAt,
                                NpcOwner = npcInfo.Owner,
                                Group = npcInfo.Group,
                                CurrentScene = scene
                            };
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

        private void OnSceneRemoved(SceneInterface scene)
        {
            scene.LoginControl.OnLoginsEnabled -= LoginControl_OnLoginsEnabled;
            m_KnownScenes.Remove(scene.ID);
            var removeList = new Dictionary<UUID, NpcAgent>();
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
                m_NonpersistentProfileService?.Remove(UUID.Zero, kvp.Key);
                NpcAgent npc = kvp.Value;
                IObject obj = npc.SittingOnObject;
                var presenceInfo = new NpcPresenceInfo()
                {
                    Npc = npc.Owner,
                    Owner = npc.NpcOwner,
                    Position = npc.Position,
                    LookAt = npc.LookAt,
                    Group = npc.Group,
                    RegionID = npc.SceneID,
                    SittingOnObjectID = obj != null ? obj.ID : UUID.Zero
                };
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

            var npcId = new UUI()
            {
                ID = UUID.Random,
                FirstName = firstName,
                LastName = lastName
            };
            if (m_KnownScenes.TryGetValue(sceneid, out scene))
            {
                var agent = new NpcAgent(npcId, null, agentServiceList)
                {
                    NpcOwner = owner,
                    Group = group
                };
                try
                {
                    m_NpcAgents.Add(agent.ID, agent);
                    var npcInfo = new NpcPresenceInfo()
                    {
                        RegionID = sceneid,
                        Npc = agent.Owner,
                        Owner = agent.NpcOwner,
                        Group = agent.Group,
                    };
                    inventoryService.CheckInventory(npcInfo.Npc.ID);
                    presenceService.Store(npcInfo);
                    agent.CurrentScene = scene;
                    agent.Position = position;
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

                scene.SendAgentObjectToAllAgents(agent);
                return agent;
            }

            throw new KeyNotFoundException("Scene not found");
        }

        private void RemoveNpcData(NpcAgent npc)
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
                npc.NpcPresenceService.Remove(UUID.Zero, npcId);
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
        private void ShowNpcsCommand(List<string> args, Main.Common.CmdIO.TTY io, UUID limitedToScene)
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
                var sb = new StringBuilder("NPCs:\n----------------------------------------------\n");
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

        private void CreateNpcCommand(List<string> args, Main.Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID ncid;
            UGI group = UGI.Unknown;
            UUI owner = UUI.Unknown;
            if (args[0] == "help" || args.Count < 5 || !UUID.TryParse(args[4], out ncid))
            {
                io.Write("create npc <firstname> <lastname> <notecardid> [params...] - Remove NPC\n" +
                    "    owner <owner>\n" + 
                    "    group <group>\n" +
                    "    position <position>\n" +
                    "    notecard <assetid>\n" +
                    "    persistent\n" +
                    "    senseasagent");
                return;
            }
            UUID sceneId;

            if(limitedToScene != UUID.Zero)
            {
                sceneId = limitedToScene;
            }
            else if(io.SelectedScene == UUID.Zero)
            {
                io.Write("Please select a region first");
                return;
            }
            else
            {
                sceneId = io.SelectedScene;
            }

            NpcOptions options = NpcOptions.None;
            Vector3 position = new Vector3(128, 128, 23);

            SceneInterface scene;
            if (!m_KnownScenes.TryGetValue(sceneId, out scene))
            {
                io.Write("Scene not found");
                return;
            }

            owner = scene.Owner;

            UUID groupid;

            for (int argi = 4; argi < args.Count; ++argi)
            {
                switch(args[argi])
                {
                    case "owner":
                        if (!scene.AvatarNameService.TranslateToUUI(args[argi + 1], out owner))
                        {
                            io.WriteFormatted("{0} is not a valid owner.", args[argi + 1]);
                            return;
                        }
                        break;

                    case "group":
                        if(scene.GroupsNameService == null)
                        {
                            io.WriteFormatted("Groups not enabled");
                            return;
                        }
                        else if(++argi >= args.Count)
                        {
                            io.WriteFormatted("Missing group id");
                            return;
                        }
                        else if(!UUID.TryParse(args[argi], out groupid))
                        {
                            io.WriteFormatted("Invalid group id {0}", args[argi]);
                            return;
                        }
                        else if(!scene.GroupsNameService.TryGetValue(groupid, out group))
                        {
                            io.WriteFormatted("Invalid group id {0}", groupid);
                            return;
                        }
                        break;

                    case "position":
                        if(++argi < args.Count)
                        {
                            if(!Vector3.TryParse(args[argi], out position))
                            {
                                position = new Vector3(128, 128, 23);
                            }
                        }
                        break;

                    case "persistent":
                        options |= NpcOptions.Persistent;
                        break;

                    case "senseasagent":
                        options |= NpcOptions.SenseAsAgent;
                        break;
                }
            }

            AssetData asset;
            if(!scene.AssetService.TryGetValue(ncid, out asset))
            {
                io.Write("Notecard not found");
                return;
            }
            if(asset.Type != AssetType.Notecard)
            {
                io.Write("Not a notecard");
                return;
            }

            Notecard nc;
            try
            {
                nc = new Notecard(asset);
            }
            catch
            {
                io.Write("Not a valid notecard");
                return;
            }

            NpcAgent agent = CreateNpc(sceneId, owner, group, args[2], args[3], position, nc, options);
            io.WriteFormatted("Npc {0} {1} ({2}) created", agent.FirstName, agent.LastName, agent.ID);
        }

        private void RemoveNpcCommand(List<string> args, Main.Common.CmdIO.TTY io, UUID limitedToScene)
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
        private void HandleRemoveNpc(HttpRequest req, Map jsondata)
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
        private void HandleGetNpc(HttpRequest req,Map jsondata)
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

            UUI uui = agent.Owner;
            var npcid = new Map
            {
                { "firstname", uui.FirstName },
                { "lastname", uui.LastName },
                { "id", uui.ID }
            };
            uui = agent.NpcOwner;
            var npcowner = new Map
            {
                { "fullname", uui.FullName },
                { "firstname", uui.FirstName },
                { "lastname", uui.LastName },
                { "id", uui.ID }
            };
            if (uui.HomeURI != null)
            {
                npcowner.Add("homeuri", uui.HomeURI);
            }

            var npcdata = new Map
            {
                { "uui", npcid },
                { "owner", npcowner },
                { "persistent", (m_NpcPresenceService != null && m_NpcPresenceService[agent.Owner.ID].Count != 0) }
            };
            m_AdminWebIF.SuccessResponse(req, npcdata);
        }

        [AdminWebIfRequiredRight("npcs.view")]
        private void HandleShowNpcs(HttpRequest req, Map jsondata)
        {
            SceneInterface scene = null;
            if (!jsondata.ContainsKey("regionid") ||
                !m_KnownScenes.TryGetValue(jsondata["regionid"].AsUUID, out scene))
            {
                m_AdminWebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }

            var npcs = new AnArray();
            foreach (NpcAgent agent in m_NpcAgents.Values)
            {
                if (agent.CurrentScene != scene)
                {
                    continue;
                }
                UUI uui = agent.Owner;
                var npcid = new Map
                {
                    { "firstname", uui.FirstName },
                    { "lastname", uui.LastName },
                    { "id", uui.ID }
                };
                uui = agent.NpcOwner;
                var npcowner = new Map
                {
                    { "fullname", uui.FullName },
                    { "firstname", uui.FirstName },
                    { "lastname", uui.LastName },
                    { "id", uui.ID }
                };
                if (uui.HomeURI != null)
                {
                    npcowner.Add("homeuri", uui.HomeURI);
                }

                var npcdata = new Map
                {
                    { "uui", npcid },
                    { "owner", npcowner },
                    { "persistent", (m_NpcPresenceService != null && m_NpcPresenceService[agent.Owner.ID].Count != 0) }
                };
                npcs.Add(npcdata);
            }
            var res = new Map
            {
                ["npcs"] = npcs
            };
            m_AdminWebIF.SuccessResponse(req, res);
        }
        #endregion
    }
}