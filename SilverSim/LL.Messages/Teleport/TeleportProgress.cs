// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;

namespace SilverSim.LL.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportProgress)]
    [Reliable]
    [Trusted]
    public class TeleportProgress : Message
    {
        public UUID AgentID;
        public TeleportFlags TeleportFlags;
        public string Message;

        public TeleportProgress()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUInt32((UInt32)TeleportFlags);
            p.WriteStringLen8(Message);
        }
    }
}
