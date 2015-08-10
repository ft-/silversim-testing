// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Circuit
{
    [UDPMessage(MessageType.LogoutReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class LogoutReply : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public List<UUID> InventoryData = new List<UUID>();

        public LogoutReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            if (0 == InventoryData.Count)
            {
                p.WriteUInt8(1);
                p.WriteUUID(UUID.Zero);
            }
            else
            {
                p.WriteUInt8((byte)InventoryData.Count);
                foreach (UUID d in InventoryData)
                {
                    p.WriteUUID(d);
                }
            }
        }
    }
}
