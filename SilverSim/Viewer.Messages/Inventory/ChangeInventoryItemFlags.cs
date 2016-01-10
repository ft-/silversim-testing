// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
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
            public InventoryFlags Flags;
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
                d.Flags = (InventoryFlags)p.ReadUInt32();
                m.InventoryData.Add(d);
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)InventoryData.Count);
            foreach(InventoryDataEntry d in InventoryData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUInt32((uint)d.Flags);
            }
        }
    }
}
