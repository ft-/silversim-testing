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

        public static Message Decode(UDPPacket p)
        {
            TeleportFailed m = new TeleportFailed();
            m.AgentID = p.ReadUUID();
            m.Reason = p.ReadStringLen8();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                AlertInfoEntry e = new AlertInfoEntry();
                e.Message = p.ReadStringLen8();
                e.ExtraParams = p.ReadStringLen8();
                m.AlertInfo.Add(e);
            }
            return m;
        }
    }
}
