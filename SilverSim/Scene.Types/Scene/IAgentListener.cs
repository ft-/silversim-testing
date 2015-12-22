// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;

namespace SilverSim.Scene.Types.Scene
{
    public interface IAgentListener
    {
        void AddedAgent(IAgent agent);
        void AgentChangedScene(IAgent agent);
        void RemovedAgent(IAgent agent);
    }
}
