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
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public interface IInventoryItemServiceInterface
    {
        #region Accessors
        /* DO NOT USE this[UUID key] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         */
        [Obsolete("Do not use this outside of Robust inventory handler", false)]
        InventoryItem this[UUID key] { get; }

        bool TryGetValue(UUID key, out InventoryItem item);
        bool ContainsKey(UUID key);

        InventoryItem this[UUID principalID, UUID key] { get; }
        bool TryGetValue(UUID principalID, UUID key, out InventoryItem item);
        bool ContainsKey(UUID principalID, UUID key);

        List<InventoryItem> this[UUID principalID, List<UUID> itemids] { get; }
        #endregion

        void Add(InventoryItem item);
        void Update(InventoryItem item);

        void Delete(UUID principalID, UUID id);
        void Move(UUID principalID, UUID id, UUID newFolder);

        /* returns list of deleted items */
        List<UUID> Delete(UUID principalID, List<UUID> ids);
    }
}
