// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Common
{
    [UDPMessage(MessageType.FeatureDisabled)]
    [Reliable]
    [Trusted]
    public class FeatureDisabled : Message
    {
        public string ErrorMessage;
        public UUID AgentID;
        public UUID TransactionID;

        public FeatureDisabled()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteStringLen8(ErrorMessage);
            p.WriteUUID(AgentID);
            p.WriteUUID(TransactionID);
        }
    }
}
