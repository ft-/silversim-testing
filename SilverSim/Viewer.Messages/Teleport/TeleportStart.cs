// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Grid;
using System;

namespace SilverSim.Viewer.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportStart)]
    [Reliable]
    [Trusted]
    public class TeleportStart : Message
    {
        public TeleportFlags TeleportFlags;

        public TeleportStart()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt32((UInt32)TeleportFlags);
        }
    }
}
