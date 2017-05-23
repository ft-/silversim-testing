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

using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Database.Memory.Inventory
{
    public partial class MemoryInventoryService : IInventoryItemServiceInterface
    {
        bool IInventoryItemServiceInterface.ContainsKey(UUID key)
        {
            foreach(var dict in m_Items.Values)
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
            foreach (var dict in m_Items.Values)
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

        List<InventoryItem> IInventoryItemServiceInterface.this[UUID principalID, List<UUID> keys]
        {
            get
            {
                var res = new List<InventoryItem>();
                foreach (var key in keys)
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
                var oldFolderID = item.ParentFolderID;
                item.ParentFolderID = toFolderID;
                IncrementVersion(principalID, oldFolderID);
                IncrementVersion(principalID, item.ParentFolderID);
                return;
            }

            throw new InventoryFolderNotStoredException(id);
        }

        private void IncrementVersion(UUID principalID, UUID folderID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder folder;
            if(m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out folder))
            {
                Interlocked.Increment(ref folder.Version);
            }
        }

        List<UUID> IInventoryItemServiceInterface.Delete(UUID principalID, List<UUID> itemids)
        {
            var deleted = new List<UUID>();
            foreach (var id in itemids)
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
