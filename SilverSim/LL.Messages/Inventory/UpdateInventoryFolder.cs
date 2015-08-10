// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.UpdateInventoryFolder)]
    [Reliable]
    [NotTrusted]
    public class UpdateInventoryFolder : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public struct InventoryDataEntry
        {
            public UUID FolderID;
            public UUID ParentID;
            public InventoryType Type;
            public string Name;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public UpdateInventoryFolder()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UpdateInventoryFolder m = new UpdateInventoryFolder();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.FolderID = p.ReadUUID();
                d.ParentID = p.ReadUUID();
                d.Type = (InventoryType)p.ReadInt8();
                d.Name = p.ReadStringLen8();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}
