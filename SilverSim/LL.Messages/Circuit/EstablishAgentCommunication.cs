// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Net;

namespace SilverSim.LL.Messages.Circuit
{
    [EventQueueGet("EstablishAgentCommunication")]
    [Trusted]
    public class EstablishAgentCommunication : Message
    {
        public UUID AgentID;
        public IPEndPoint SimIpAndPort = new IPEndPoint(0, 0);
        public string SeedCapability;
        public GridVector GridPosition;
        public GridVector RegionSize;

        public EstablishAgentCommunication()
        {

        }

        public override SilverSim.Types.IValue SerializeEQG()
        {
            SilverSim.Types.Map i = new SilverSim.Types.Map();
            i.Add("agent-id", AgentID);
            i.Add("sim-ip-and-port", SimIpAndPort.ToString());
            i.Add("seed-capability", SeedCapability);
            i.Add("region-handle", GridPosition.RegionHandle);
            i.Add("region-size-x", RegionSize.X);
            i.Add("region-size-y", RegionSize.Y);

            return i;
        }
    }
}
