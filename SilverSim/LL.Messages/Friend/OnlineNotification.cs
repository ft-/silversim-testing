// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Friend
{
    [UDPMessage(MessageType.OnlineNotification)]
    [Reliable]
    [Trusted]
    public class OnlineNotification : Message
    {
        public List<UUID> AgentIDs = new List<UUID>();

        public OnlineNotification()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt8((byte)AgentIDs.Count);
            foreach(UUID id in AgentIDs)
            {
                p.WriteUUID(id);
            }
        }
    }
}
