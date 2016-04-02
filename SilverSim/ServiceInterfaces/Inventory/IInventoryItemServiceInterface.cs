// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public interface IInventoryItemServiceInterface
    {
        #region Accessors
        /* DO NOT USE this[UUID key] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         */
        InventoryItem this[UUID key]
        {
            get;
        }

       bool TryGetValue(UUID key, out InventoryItem item);
       bool ContainsKey(UUID key);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        InventoryItem this[UUID principalID, UUID key]
        {
            get;
        }
        bool TryGetValue(UUID principalID, UUID key, out InventoryItem item);
        bool ContainsKey(UUID principalID, UUID key);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<InventoryItem> this[UUID principalID, List<UUID> itemids]
        {
            get;
        }
        #endregion

        void Add(InventoryItem item);
        void Update(InventoryItem item);

        void Delete(UUID principalID, UUID id);
        void Move(UUID principalID, UUID id, UUID newFolder);

        /* returns list of deleted items */
        List<UUID> Delete(UUID principalID, List<UUID> ids);
    }
}
