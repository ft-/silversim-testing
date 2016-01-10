// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Circuit
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

        public static Message Decode(UDPPacket p)
        {
            LogoutReply m = new LogoutReply();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            uint n = p.ReadUInt8();
            for (uint i = 0; i < n; ++i)
            {
                m.InventoryData.Add(p.ReadUUID());
            }
            return m;
        }
    }
}
