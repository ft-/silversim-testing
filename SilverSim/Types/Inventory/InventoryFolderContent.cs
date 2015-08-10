// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Types.Inventory
{
    public class InventoryFolderContent
    {
        public List<InventoryFolder> Folders = new List<InventoryFolder>();
        public List<InventoryItem> Items = new List<InventoryItem>();

        public UUID FolderID = UUID.Zero;
        public UUI Owner = UUI.Unknown;
        public int Version = 0;

        public InventoryFolderContent()
        {

        }
    }
}
