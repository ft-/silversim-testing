// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.Agent;

namespace SilverSim.Scene.Types.Agent
{
    public interface IAgentChildUpdateServiceInterface
    {
        /* <summary>do not reuse the same message in multiple connectors, both calls must be non-blocking</summary> */
        void SendMessage(ChildAgentPositionUpdate m);
        /* <summary>do not reuse the same message in multiple connectors, both calls must be non-blocking</summary> */
        void SendMessage(ChildAgentUpdate m);

        void Disconnect();
    }
}
