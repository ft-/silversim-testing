// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public interface IInventoryFolderContentServiceInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        InventoryFolderContent this[UUID principalID, UUID folderID]
        {
            get;
        }

        bool TryGetValue(UUID principalID, UUID folderID, out InventoryFolderContent inventoryFolderContent);
        bool ContainsKey(UUID principalID, UUID folderID);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        List<InventoryFolderContent> this[UUID principalID, UUID[] folderIDs]
        {
            get;
        }
    }
}
