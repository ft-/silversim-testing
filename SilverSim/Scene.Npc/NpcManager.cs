// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;

namespace SilverSim.Scene.Npc
{
    public class NpcManager : IPlugin
    {
        public NpcManager()
        {

        }

        readonly RwLockedDictionary<UUID, NpcAgent> m_NpcAgents = new RwLockedDictionary<UUID, NpcAgent>();

        public void Startup(ConfigurationLoader loader)
        {
        }

        public NpcAgent CreateNpc(string firstName, string lastName, Vector3 position, Notecard nc, NpcOptions options = NpcOptions.None)
        {
            throw new NotImplementedException();
        }

        public void RemoveNpc(UUID npcId)
        {
            throw new NotImplementedException();
        }

        public bool TryGetNpc(UUID npcId, out NpcAgent agent)
        {
            return m_NpcAgents.TryGetValue(npcId, out agent);
        }
    }
}