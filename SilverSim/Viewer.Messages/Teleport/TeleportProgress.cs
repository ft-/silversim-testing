// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;

namespace SilverSim.Viewer.Messages.Teleport
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
            p.WriteUUID(AgentID);
            p.WriteUInt32((UInt32)TeleportFlags);
            p.WriteStringLen8(Message);
        }

        public static Message Decode(UDPPacket p)
        {
            TeleportProgress m = new TeleportProgress();
            m.AgentID = p.ReadUUID();
            m.TeleportFlags = (TeleportFlags)p.ReadUInt32();
            m.Message = p.ReadStringLen8();
            return m;
        }
    }
}
