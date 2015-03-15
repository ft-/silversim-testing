/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
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

        public abstract List<InventoryItem> getActiveGestures(UUID PrincipalID);

        void verifyInventoryFolder(UUID principalID, UUID parentFolderID, string name, InventoryType type)
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
                folder.InventoryType = type;
                folder.ParentFolderID = parentFolderID;
                folder.Version = 1;
                Folder.Add(folder);
            }
        }

        public virtual void checkInventory(UUID PrincipalID)
        {
            InventoryFolder rootFolder;
            try
            {
                rootFolder = Folder[PrincipalID, InventoryType.RootFolder];
            }
            catch
            {
                rootFolder = new InventoryFolder();
                rootFolder.Owner.ID = PrincipalID;
                rootFolder.Name = "My Inventory";
                rootFolder.InventoryType = InventoryType.Folder;
                rootFolder.ParentFolderID = UUID.Zero;
                rootFolder.Version = 1;
                Folder.Add(rootFolder);
            }

            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Animations", InventoryType.Animation);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Body Parts", InventoryType.Bodypart);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Calling Cards", InventoryType.CallingCard);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Clothing", InventoryType.Clothing);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Gestures", InventoryType.Gesture);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Landmarks", InventoryType.Landmark);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Lost And Found", InventoryType.LostAndFoundFolder);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Notecards", InventoryType.Notecard);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Objects", InventoryType.Object);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Photo Album", InventoryType.SnapshotFolder);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Scripts", InventoryType.LSLText);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Sounds", InventoryType.Sound);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Textures", InventoryType.Texture);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Trash", InventoryType.TrashFolder);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Current Outfit", InventoryType.CurrentOutfitFolder);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "My Outfits", InventoryType.MyOutfitsFolder);
            verifyInventoryFolder(PrincipalID, rootFolder.ID, "Favorites", InventoryType.FavoriteFolder);
        }
        #endregion

        #region Constructor
        public InventoryServiceInterface()
        {

        }
        #endregion
    }
}
