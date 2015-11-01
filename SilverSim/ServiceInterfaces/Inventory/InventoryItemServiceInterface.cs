// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryItemServiceInterface
    {
        #region Accessors
        /* DO NOT USE this[UUID key] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         */
        public abstract InventoryItem this[UUID key]
        {
            get;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract InventoryItem this[UUID principalID, UUID key]
        {
            get;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public virtual List<InventoryItem> this[UUID principalID, List<UUID> itemids]
        {
            get
            {
                List<InventoryItem> items = new List<InventoryItem>();
                foreach(UUID itemid in itemids)
                {
                    try
                    {
                        items.Add(this[principalID, itemid]);
                    }
                    catch
                    {

                    }
                }
                return items;
            }
        }

        #endregion

        public abstract void Add(InventoryItem item);
        public abstract void Update(InventoryItem item);

        public abstract void Delete(UUID principalID, UUID id);
        public abstract void Move(UUID principalID, UUID id, UUID newFolder);

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public virtual List<UUID> Delete(UUID principalID, List<UUID> ids)
        {
            List<UUID> deleted = new List<UUID>();
            foreach(UUID id in ids)
            {
                try
                {
                    Delete(principalID, id);
                    deleted.Add(id);
                }
                catch
                {

                }
            }
            return deleted;
        }

        #region Constructor
        public InventoryItemServiceInterface()
        {

        }
        #endregion
    }
}
