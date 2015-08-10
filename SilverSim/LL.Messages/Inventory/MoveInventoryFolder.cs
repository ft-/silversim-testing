// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.MoveInventoryFolder)]
    [Reliable]
    [NotTrusted]
    public class MoveInventoryFolder : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public bool Stamp;
        public struct InventoryDataEntry
        {
            public UUID FolderID;
            public UUID ParentID;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public MoveInventoryFolder()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MoveInventoryFolder m = new MoveInventoryFolder();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Stamp = p.ReadBoolean();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.FolderID = p.ReadUUID();
                d.ParentID = p.ReadUUID();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}
