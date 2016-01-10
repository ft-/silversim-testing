// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportFailed)]
    [Reliable]
    [Trusted]
    public class TeleportFailed : Message
    {
        public UUID AgentID;
        public string Reason;

        public struct AlertInfoEntry
        {
            public string Message;
            public string ExtraParams;
        }

        public List<AlertInfoEntry> AlertInfo = new List<AlertInfoEntry>();

        public TeleportFailed()
        {
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteStringLen8(Reason);
            p.WriteUInt8((byte)AlertInfo.Count);
            foreach(AlertInfoEntry e in AlertInfo)
            {
                p.WriteStringLen8(e.Message);
                p.WriteStringLen8(e.ExtraParams);
            }
        }
    }
}
