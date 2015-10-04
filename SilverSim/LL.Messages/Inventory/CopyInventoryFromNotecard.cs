// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.CopyInventoryFromNotecard)]
    [Reliable]
    [NotTrusted]
    public class CopyInventoryFromNotecard : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID NotecardItemID;
        public UUID ObjectID;

        public struct InventoryDataEntry
        {
            public UUID ItemID;
            public UUID FolderID;
        }

        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public CopyInventoryFromNotecard()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            CopyInventoryFromNotecard m = new CopyInventoryFromNotecard();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.NotecardItemID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.ItemID = p.ReadUUID();
                d.FolderID = p.ReadUUID();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}
