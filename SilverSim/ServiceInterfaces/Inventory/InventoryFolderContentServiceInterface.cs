// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryFolderContentServiceInterface
    {
        public InventoryFolderContentServiceInterface()
        {

        }

        public abstract InventoryFolderContent this[UUID principalID, UUID folderID]
        {
            get;
        }


        public virtual List<InventoryFolderContent> this[UUID principalID, UUID[] folderIDs]
        {
            get
            {
                List<InventoryFolderContent> contents = new List<InventoryFolderContent>();
                foreach(UUID folderID in folderIDs)
                {
                    try
                    {
                        contents.Add(this[principalID, folderID]);
                    }
                    catch
                    {

                    }
                }
                return contents;
            }
        }
    }
}
