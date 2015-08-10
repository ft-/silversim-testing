// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.CreateInventoryFolder)]
    [Reliable]
    [NotTrusted]
    public class CreateInventoryFolder : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID FolderID;
        public UUID ParentFolderID;
        public InventoryType FolderType;
        public string FolderName;

        public CreateInventoryFolder()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            CreateInventoryFolder m = new CreateInventoryFolder();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.FolderID = p.ReadUUID();
            m.ParentFolderID = p.ReadUUID();
            m.FolderType = (InventoryType)p.ReadInt8();
            m.FolderName = p.ReadStringLen8();

            return m;
        }
    }
}
