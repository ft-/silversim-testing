// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;

namespace SilverSim.Viewer.Core
{
    public interface ITriggerOnRootAgentActions : IProtocolExtender
    {
        void TriggerOnRootAgent(UUID agent, SceneInterface scene);
    }
}
