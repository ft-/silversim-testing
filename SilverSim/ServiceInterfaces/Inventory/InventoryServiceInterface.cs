// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryServiceInterface
    {
        #region Accessors
        public abstract InventoryFolderServiceInterface Folder
        {
            get;
        }

        public abstract InventoryItemServiceInterface Item
        {
            get;
        }

        public abstract List<InventoryItem> GetActiveGestures(UUID principalID);

        void VerifyInventoryFolder(UUID principalID, UUID parentFolderID, string name, AssetType type)
        {
            InventoryFolder folder;
            try
            {
                folder = Folder[principalID, type];
            }
            catch
            {
                folder = new InventoryFolder();
                folder.Owner.ID = principalID;
                folder.Name = name;
                folder.InventoryType = (InventoryType)type;
                folder.ParentFolderID = parentFolderID;
                folder.Version = 1;
                Folder.Add(folder);
            }
        }

        public virtual void CheckInventory(UUID principalID)
        {
            InventoryFolder rootFolder;
            try
            {
                rootFolder = Folder[principalID, AssetType.RootFolder];
            }
            catch
            {
                rootFolder = new InventoryFolder();
                rootFolder.Owner.ID = principalID;
                rootFolder.Name = "My Inventory";
                rootFolder.InventoryType = InventoryType.Folder;
                rootFolder.ParentFolderID = UUID.Zero;
                rootFolder.Version = 1;
                Folder.Add(rootFolder);
            }

            VerifyInventoryFolder(principalID, rootFolder.ID, "Animations", AssetType.Animation);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Body Parts", AssetType.Bodypart);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Calling Cards", AssetType.CallingCard);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Clothing", AssetType.Clothing);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Gestures", AssetType.Gesture);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Landmarks", AssetType.Landmark);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Lost And Found", AssetType.LostAndFoundFolder);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Notecards", AssetType.Notecard);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Objects", AssetType.Object);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Photo Album", AssetType.SnapshotFolder);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Scripts", AssetType.LSLText);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Sounds", AssetType.Sound);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Textures", AssetType.Texture);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Trash", AssetType.TrashFolder);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Current Outfit", AssetType.CurrentOutfitFolder);
            VerifyInventoryFolder(principalID, rootFolder.ID, "My Outfits", AssetType.MyOutfitsFolder);
            VerifyInventoryFolder(principalID, rootFolder.ID, "Favorites", AssetType.FavoriteFolder);
        }
        #endregion

        #region Constructor
        public InventoryServiceInterface()
        {

        }
        #endregion
    }
}
