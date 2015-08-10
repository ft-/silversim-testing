// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Inventory;

namespace SilverSim.Types.Asset.Format
{
    public class NotecardInventoryItem : InventoryItem
    {
        #region Fields
        public uint ExtCharIndex = 0;
        #endregion

        #region Constructor
        public NotecardInventoryItem()
        {

        }

        public NotecardInventoryItem(InventoryItem item)
        {
            AssetID = new UUID(item.AssetID);
            AssetType = item.AssetType;
            CreationDate = item.CreationDate;
            Creator = new UUI(item.Creator);
            Description = item.Description;
            Flags = item.Flags;
            Group = new UGI(item.Group);
            IsGroupOwned = item.IsGroupOwned;
            ID = new UUID(item.ID);
            InventoryType = item.InventoryType;
            LastOwner = new UUI(item.LastOwner);
            Name = item.Name;
            Owner = new UUI(item.Owner);
            ParentFolderID = new UUID(item.ParentFolderID);
            Permissions = item.Permissions;
            SaleInfo = item.SaleInfo;
        }

        #endregion
    }
}
