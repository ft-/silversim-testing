// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public interface IInventoryFolderServiceInterface
    {
        #region Accessors
        /* DO NOT USE this[UUID key] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         */
        InventoryFolder this[UUID key]
        {
            get;
        }
        bool TryGetValue(UUID key, out InventoryFolder folder);
        bool ContainsKey(UUID key);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        InventoryFolder this[UUID principalID, UUID key]
        {
            get;
        }
        bool TryGetValue(UUID principalID, UUID key, out InventoryFolder folder);
        bool ContainsKey(UUID principalID, UUID key);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        InventoryFolder this[UUID principalID, AssetType type]
        {
            get;
        }
        bool TryGetValue(UUID principalID, AssetType type, out InventoryFolder folder);
        bool ContainsKey(UUID principalID, AssetType type);

        IInventoryFolderContentServiceInterface Content
        {
            get;
        }

        List<InventoryFolder> GetFolders(UUID principalID, UUID key);
        List<InventoryItem> GetItems(UUID principalID, UUID key);
        #endregion

        #region Methods
        void Add(InventoryFolder folder);
        void Update(InventoryFolder folder);
        void Move(UUID principalID, UUID folderID, UUID toFolderID);
        void Delete(UUID principalID, UUID folderID);
        /* DO NOT USE Purge[UUID folderID] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         * Only required path to support this is from Robust Inventory handler towards database connector.
         */
        void Purge(UUID folderID);
        void Purge(UUID principalID, UUID folderID);
        void IncrementVersion(UUID principalID, UUID folderID);
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        /* returns list of deleted folders */
        List<UUID> Delete(UUID principalID, List<UUID> folderIDs);
        #endregion
    }
}
