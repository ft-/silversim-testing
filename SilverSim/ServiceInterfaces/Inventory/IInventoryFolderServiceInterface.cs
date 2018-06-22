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
    public interface IInventoryFolderServiceInterface : This.IInventoryFolderServiceThisInterface
    {
        #region Accessors
        /* DO NOT USE this[UUID key] and relatives anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         */
        [Obsolete("Do not use this outside of Robust inventory handler", false)]
        bool TryGetValue(UUID key, out InventoryFolder folder);
        [Obsolete("Do not use this outside of Robust inventory handler", false)]
        bool ContainsKey(UUID key);

        bool TryGetValue(UUID principalID, UUID key, out InventoryFolder folder);
        bool ContainsKey(UUID principalID, UUID key);

        bool TryGetValue(UUID principalID, AssetType type, out InventoryFolder folder);
        bool ContainsKey(UUID principalID, AssetType type);

        IInventoryFolderContentServiceInterface Content { get; }

        List<InventoryFolder> GetFolders(UUID principalID, UUID key);
        List<InventoryItem> GetItems(UUID principalID, UUID key);
        #endregion

        #region Methods
        void Add(InventoryFolder folder);
        void Update(InventoryFolder folder);
        void Move(UUID principalID, UUID folderID, UUID toFolderID);
        void Delete(UUID principalID, UUID folderID);
        InventoryTree Copy(UUID principalID, UUID folderID, UUID toFolderID);
        /* DO NOT USE Purge[UUID folderID] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         * Only required path to support this is from Robust Inventory handler towards database connector.
         */
        void Purge(UUID folderID);
        void Purge(UUID principalID, UUID folderID);
        void IncrementVersion(UUID principalID, UUID folderID);
        /* returns list of deleted folders */
        List<UUID> Delete(UUID principalID, List<UUID> folderIDs);
        #endregion
    }
}
