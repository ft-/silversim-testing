// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryServiceInterface
    {
        public abstract void Remove(UUID scopeID, UUID accountID);

        #region Accessors
        public abstract IInventoryFolderServiceInterface Folder { get; }

        public abstract IInventoryItemServiceInterface Item { get; }

        public abstract List<InventoryItem> GetActiveGestures(UUID principalID);

        private void VerifyInventoryFolder(UUID principalID, UUID parentFolderID, string name, AssetType type)
        {
            InventoryFolder folder;
            try
            {
                folder = Folder[principalID, type];
            }
            catch(KeyNotFoundException)
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

        public virtual List<InventoryFolder> GetInventorySkeleton(UUID principalID)
        {
            throw new NotSupportedException("InventoryServiceInterface.GetInventorySkeleton");
        }

        public virtual void CheckInventory(UUID principalID)
        {
            InventoryFolder rootFolder;
            try
            {
                rootFolder = Folder[principalID, AssetType.RootFolder];
            }
            catch(KeyNotFoundException)
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
    }
}
