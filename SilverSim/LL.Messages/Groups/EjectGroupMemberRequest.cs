// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [Zerocoded]
    [Reliable]
    [UDPMessage(MessageType.EjectGroupMemberRequest)]
    [NotTrusted]
    public class EjectGroupMemberRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID EjecteeID = UUID.Zero;

        public EjectGroupMemberRequest()
        {

        }

        public override MessageType Number
        {
            get 
            {
                return MessageType.EjectGroupMemberRequest;
            }
        }
    }
}
