// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.FetchInventory)]
    [Reliable]
    [NotTrusted]
    public class FetchInventory : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public struct InventoryDataEntry
        {
            public UUID OwnerID;
            public UUID ItemID;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public FetchInventory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            FetchInventory m = new FetchInventory();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            uint i;
            uint c = p.ReadUInt8();
            for (i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.OwnerID = p.ReadUUID();
                d.ItemID = p.ReadUUID();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}
