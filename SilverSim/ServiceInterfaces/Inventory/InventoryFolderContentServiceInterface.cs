// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryFolderContentServiceInterface
    {
        public InventoryFolderContentServiceInterface()
        {

        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract InventoryFolderContent this[UUID principalID, UUID folderID]
        {
            get;
        }

        public abstract bool TryGetValue(UUID principalID, UUID folderID, out InventoryFolderContent inventoryFolderContent);
        public abstract bool ContainsKey(UUID principalID, UUID folderID);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
