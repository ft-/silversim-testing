// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryFolderServiceInterface
    {
        #region Accessors
        /* DO NOT USE this[UUID key] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         */
        public abstract InventoryFolder this[UUID key]
        {
            get;
        }

        public abstract InventoryFolder this[UUID principalID, UUID key]
        {
            get;
        }

        public abstract InventoryFolder this[UUID principalID, AssetType type]
        {
            get;
        }


        public virtual InventoryFolderContentServiceInterface Content
        {
            get
            {
                return new DefaultInventoryFolderContentService(this);
            }
        }

        public abstract List<InventoryFolder> GetFolders(UUID principalID, UUID key);
        public abstract List<InventoryItem> GetItems(UUID principalID, UUID key);

        public virtual List<InventoryFolder> GetInventorySkeleton(UUID principalID)
        {
            throw new NotSupportedException(GetType().FullName + ": getInventorySkeleton not supported");
        }
        #endregion

        #region Methods
        public abstract void Add(InventoryFolder folder);
        public abstract void Update(InventoryFolder folder);
        public abstract void Move(UUID principalID, UUID folderID, UUID toFolderID);
        public abstract void Delete(UUID principalID, UUID folderID);
        /* DO NOT USE Purge[UUID folderID] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         * Only required path to support this is from Robust Inventory handler towards database connector.
         */
        public abstract void Purge(UUID folderID);
        public abstract void Purge(UUID principalID, UUID folderID);
        public abstract void IncrementVersion(UUID principalID, UUID folderID);
        public virtual List<UUID> Delete(UUID principalID, List<UUID> folderIDs)
        {
            List<UUID> deleted = new List<UUID>();
            foreach(UUID folderID in folderIDs)
            {
                try
                {
                    Delete(principalID, folderID);
                    deleted.Add(folderID);
                }
                catch
                {

                }
            }
            return deleted;
        }
        #endregion

        #region Constructor
        public InventoryFolderServiceInterface()
        {
        }
        #endregion
    }
}
