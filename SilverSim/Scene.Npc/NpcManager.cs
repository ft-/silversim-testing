// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System.Collections.Generic;

namespace SilverSim.Scene.Npc
{
    public class NpcManager : IPlugin
    {
        string m_InventoryServiceName;
        string m_PresenceServiceName;
        InventoryServiceInterface m_InventoryService;
        PresenceServiceInterface m_PresenceService;

        public NpcManager(IConfig ownConfig)
        {
            m_InventoryServiceName = ownConfig.GetString("InventoryService", string.Empty);
            m_PresenceServiceName = ownConfig.GetString("PresenceService", string.Empty);
        }

        readonly RwLockedDictionary<UUID, NpcAgent> m_NpcAgents = new RwLockedDictionary<UUID, NpcAgent>();
        AgentServiceList m_AgentServices = new AgentServiceList();

        public void Startup(ConfigurationLoader loader)
        {
            if (m_InventoryServiceName != null && m_PresenceServiceName != null)
            {
                m_InventoryService = loader.GetService<InventoryServiceInterface>(m_InventoryServiceName);
                m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
                m_AgentServices.Add(m_InventoryService);
                m_AgentServices.Add(m_PresenceService);
            }
            loader.Scenes.OnRegionAdd += OnSceneAdded;
            loader.Scenes.OnRegionRemove += OnSceneRemoved;
        }

        readonly RwLockedDictionary<UUID, SceneInterface> m_KnownScenes = new RwLockedDictionary<UUID, SceneInterface>();
        void OnSceneAdded(SceneInterface scene)
        {
            m_KnownScenes.Add(scene.ID, scene);
        }

        void OnSceneRemoved(SceneInterface scene)
        {
            m_KnownScenes.Remove(scene.ID);
            List<UUID> removeList = new List<UUID>();
            foreach(NpcAgent agent in m_NpcAgents.Values)
            {
                if(agent.CurrentScene == scene)
                {
                    removeList.Add(agent.ID);
                }
            }

            foreach(UUID id in removeList)
            {
                m_NpcAgents.Remove(id);
            }
        }

        public NpcAgent CreateNpc(UUID sceneid, UUI owner, UGI group, string firstName, string lastName, Vector3 position, Notecard nc, NpcOptions options = NpcOptions.None)
        {
            SceneInterface scene;
            if (m_KnownScenes.TryGetValue(sceneid, out scene))
            {
                NpcAgent agent = new NpcAgent(UUID.Random, firstName, lastName, null, m_AgentServices);
                agent.NpcOwner = owner;
                agent.Group = group;
                try
                {
                    m_NpcAgents.Add(agent.ID, agent);
                    scene.Add(agent);
                    return agent;
                }
                catch
                {
                    m_NpcAgents.Remove(agent.ID);
                    throw;
                }
            }

            throw new KeyNotFoundException("Scene not found");
        }

        public bool RemoveNpc(UUID npcId)
        {
            NpcAgent npc;
            if(m_NpcAgents.Remove(npcId, out npc))
            {
                npc.CurrentScene.Remove(npc);
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