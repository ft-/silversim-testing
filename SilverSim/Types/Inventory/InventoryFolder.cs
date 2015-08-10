// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Inventory
{
    public class InventoryFolder
    {
        #region InventoryFolder Data
        public UUID ID = UUID.Zero;
        public UUID ParentFolderID = UUID.Zero;
        public string Name = string.Empty;
        public InventoryType InventoryType = InventoryType.Unknown;
        public UUI Owner = UUI.Unknown;
        public int Version = 1;
        #endregion

        #region Constructors
        public InventoryFolder()
        {
            ID = UUID.Random;
        }

        public InventoryFolder(UUID id)
        {
            ID = id;
        }
        #endregion
    }
}
