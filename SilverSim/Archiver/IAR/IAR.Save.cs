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

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Archiver.IAR
{
    public static partial class IAR
    {
        [Flags]
        public enum SaveOptions
        {
            NoAssets = 0x00000001
        }

        public static void Save(
            UUI principal, 
            InventoryServiceInterface inventoryService,
            AssetServiceInterface assetService,
            AvatarNameServiceInterface nameService,
            SaveOptions options,
            string fileName,
            string frompath)
        {
            UUID parentFolder;
            parentFolder = inventoryService.Folder[principal.ID, AssetType.RootFolder].ID;

            if (!frompath.StartsWith("/"))
            {
                throw new InvalidInventoryPathException();
            }
            foreach (string pathcomp in frompath.Substring(1).Split('/'))
            {
                List<InventoryFolder> childfolders = inventoryService.Folder.getFolders(principal.ID, parentFolder);
                int idx;
                for (idx = 0; idx < childfolders.Count; ++idx)
                {
                    if (pathcomp.ToLower() == childfolders[idx].Name.ToLower())
                    {
                        break;
                    }
                }

                if (idx == childfolders.Count)
                {
                    throw new InvalidInventoryPathException();
                }

                parentFolder = childfolders[idx].ID;
            }

        }
    }
}
