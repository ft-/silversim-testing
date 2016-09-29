// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Database.Memory.Inventory
{
    public partial class MemoryInventoryService : IInventoryItemServiceInterface
    {
        bool IInventoryItemServiceInterface.ContainsKey(UUID key)
        {
            foreach(RwLockedDictionary<UUID, InventoryItem> dict in m_Items.Values)
            {
                if(dict.ContainsKey(key))
                {
                    return true;
                }
            }

            return false;
        }

        bool IInventoryItemServiceInterface.TryGetValue(UUID key, out InventoryItem item)
        {
            foreach (RwLockedDictionary<UUID, InventoryItem> dict in m_Items.Values)
            {
                if (dict.TryGetValue(key, out item))
                {
                    return true;
                }
            }

            item = default(InventoryItem);
            return false;
        }

        InventoryItem IInventoryItemServiceInterface.this[UUID key]
        {
            get
            {
                InventoryItem item;
                if(!Item.TryGetValue(key, out item))
                {
                    throw new KeyNotFoundException();
                }
                return item;
            }
        }


        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        List<InventoryItem> IInventoryItemServiceInterface.this[UUID principalID, List<UUID> keys]
        {
            get
            {
                List<InventoryItem> res = new List<InventoryItem>();
                foreach (UUID key in keys)
                {
                    try
                    {
                        res.Add(Item[principalID, key]);
                    }
                    catch
                    {
                        /* nothing to do here */
                    }
                }

                return res;
            }
        }

        bool IInventoryItemServiceInterface.ContainsKey(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryItem> dict;
            return m_Items.TryGetValue(principalID, out dict) && dict.ContainsKey(key);
        }

        bool IInventoryItemServiceInterface.TryGetValue(UUID principalID, UUID key, out InventoryItem item)
        {
            RwLockedDictionary<UUID, InventoryItem> dict;
            item = default(InventoryItem);
            if(m_Items.TryGetValue(principalID, out dict) && dict.TryGetValue(key, out item))
            {
                item = new InventoryItem(item);
                return true;
            }
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        InventoryItem IInventoryItemServiceInterface.this[UUID principalID, UUID key]
        {
            get 
            {
                InventoryItem item;
                if(!Item.TryGetValue(principalID, key, out item))
                {
                    throw new KeyNotFoundException();
                }
                return item;
            }
        }

        void IInventoryItemServiceInterface.Add(InventoryItem item)
        {
            m_Items[item.Owner.ID].Add(item.ID, new InventoryItem(item));
            IncrementVersion(item.Owner.ID, item.ParentFolderID);
        }

        void IInventoryItemServiceInterface.Update(InventoryItem item)
        {
            RwLockedDictionary<UUID, InventoryItem> itemSet;
            InventoryItem storedItem;
            if(m_Items.TryGetValue(item.Owner.ID, out itemSet) &&
                itemSet.TryGetValue(item.ID, out storedItem))
            {
                storedItem.AssetID = item.AssetID;
                storedItem.Name = item.Name;
                storedItem.Description = item.Description;
                storedItem.Permissions.Base = item.Permissions.Base;
                storedItem.Permissions.Current = item.Permissions.Current;
                storedItem.Permissions.EveryOne = item.Permissions.EveryOne;
                storedItem.Permissions.NextOwner = item.Permissions.NextOwner;
                storedItem.Permissions.Group = item.Permissions.Group;
                storedItem.SaleInfo.Price = item.SaleInfo.Price;
                storedItem.SaleInfo.Type = item.SaleInfo.Type;
                IncrementVersion(item.Owner.ID, item.ParentFolderID);
            }
        }

        void IInventoryItemServiceInterface.Delete(UUID principalID, UUID id)
        {
            InventoryItem item;
            RwLockedDictionary<UUID, InventoryItem> itemSet;
            if (m_Items.TryGetValue(principalID, out itemSet) &&
                itemSet.Remove(id, out item))
            {
                IncrementVersion(principalID, item.ParentFolderID);
                return;
            }
            throw new InventoryItemNotFoundException(id);
        }

        void IInventoryItemServiceInterface.Move(UUID principalID, UUID id, UUID toFolderID)
        {
            InventoryItem item;
            RwLockedDictionary<UUID, InventoryItem> itemSet;
            if (m_Items.TryGetValue(principalID, out itemSet) &&
                itemSet.TryGetValue(id, out item))
            {
                UUID oldFolderID = item.ParentFolderID;
                item.ParentFolderID = toFolderID;
                IncrementVersion(principalID, oldFolderID);
                IncrementVersion(principalID, item.ParentFolderID);
                return;
            }

            throw new InventoryFolderNotStoredException(id);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void IncrementVersion(UUID principalID, UUID folderID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder folder;
            if(m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out folder))
            {
                Interlocked.Increment(ref folder.Version);
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        List<UUID> IInventoryItemServiceInterface.Delete(UUID principalID, List<UUID> itemids)
        {
            List<UUID> deleted = new List<UUID>();
            foreach (UUID id in itemids)
            {
                try
                {
                    Item.Delete(principalID, id);
                    deleted.Add(id);
                }
                catch
                {
                    /* nothing else to do */
                }
            }
            return deleted;
        }
    }
}
