// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.ChangeInventoryItemFlags)]
    [Reliable]
    [NotTrusted]
    public class ChangeInventoryItemFlags : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public struct InventoryDataEntry
        {
            public UUID ItemID;
            public UInt32 Flags;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public ChangeInventoryItemFlags()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ChangeInventoryItemFlags m = new ChangeInventoryItemFlags();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.ItemID = p.ReadUUID();
                d.Flags = p.ReadUInt32();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}
