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
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SilverSim.Database.Memory.Inventory
{
    public partial class MemoryInventoryService : IInventoryFolderServiceInterface
    {
        bool IInventoryFolderServiceInterface.TryGetValue(UUID key, out InventoryFolder folder)
        {
            foreach(RwLockedDictionary<UUID, InventoryFolder> dict in m_Folders.Values)
            {
                if(dict.TryGetValue(key, out folder))
                {
                    folder = new InventoryFolder(folder);
                    return true;
                }
            }
            folder = default(InventoryFolder);
            return false;
        }

        bool IInventoryFolderServiceInterface.ContainsKey(UUID key)
        {
            foreach (RwLockedDictionary<UUID, InventoryFolder> dict in m_Folders.Values)
            {
                if (dict.ContainsKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        InventoryFolder IInventoryFolderServiceInterface.this[UUID key]
        {
            get
            {
                InventoryFolder folder;
                if(!Folder.TryGetValue(key, out folder))
                {
                    throw new InventoryFolderNotFoundException(key);
                }
                return folder;
            }
        }

        bool IInventoryFolderServiceInterface.TryGetValue(UUID principalID, UUID key, out InventoryFolder folder)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            if(m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(key, out folder))
            {
                folder = new InventoryFolder(folder);
                return true;
            }

            folder = default(InventoryFolder);
            return false;
        }

        bool IInventoryFolderServiceInterface.ContainsKey(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Folders.TryGetValue(principalID, out folderSet) && folderSet.ContainsKey(key);
        }

        InventoryFolder IInventoryFolderServiceInterface.this[UUID principalID, UUID key]
        {
            get
            {
                InventoryFolder folder;
                if(!Folder.TryGetValue(principalID, key, out folder))
                {
                    throw new InventoryFolderNotFoundException(key);
                }
                return folder;
            }
        }

        bool IInventoryFolderServiceInterface.ContainsKey(UUID principalID, AssetType type)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            if(m_Folders.TryGetValue(principalID, out folderSet))
            {
                if(type == AssetType.RootFolder)
                {
                    foreach(var folder in folderSet.Values)
                    {
                        if(folder.ParentFolderID == UUID.Zero)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    foreach(var folder in folderSet.Values)
                    {
                        if(folder.DefaultType == type)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IInventoryFolderServiceInterface.TryGetValue(UUID principalID, AssetType type, out InventoryFolder folder)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            if (m_Folders.TryGetValue(principalID, out folderSet))
            {
                if (type == AssetType.RootFolder)
                {
                    foreach (var sfolder in folderSet.Values)
                    {
                        if (sfolder.ParentFolderID == UUID.Zero)
                        {
                            folder = new InventoryFolder(sfolder);
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (var sfolder in folderSet.Values)
                    {
                        if (sfolder.DefaultType == type)
                        {
                            folder = new InventoryFolder(sfolder);
                            return true;
                        }
                    }
                }
            }

            folder = default(InventoryFolder);
            return false;
        }

        InventoryFolder IInventoryFolderServiceInterface.this[UUID principalID, AssetType type]
        {
            get
            {
                InventoryFolder folder;
                if(Folder.TryGetValue(principalID, type, out folder))
                {
                    return folder;
                }

                throw new InventoryFolderTypeNotFoundException(type);
            }
        }

        List<InventoryFolder> IInventoryFolderServiceInterface.GetFolders(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Folders.TryGetValue(principalID, out folderSet) ?
                new List<InventoryFolder>(from folder in folderSet.Values where folder.ParentFolderID == key select new InventoryFolder(folder)) :
                new List<InventoryFolder>();
        }

        public override List<InventoryFolder> GetInventorySkeleton(UUID principalID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Folders.TryGetValue(principalID, out folderSet) ?
                new List<InventoryFolder>(from folder in folderSet.Values where true select new InventoryFolder(folder)) :
                new List<InventoryFolder>();
        }

        List<InventoryItem> IInventoryFolderServiceInterface.GetItems(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryItem> itemSet;
            return m_Items.TryGetValue(principalID, out itemSet) ?
                new List<InventoryItem>(from item in itemSet.Values where item.ParentFolderID == key select new InventoryItem(item)) :
                new List<InventoryItem>();
        }

        void IInventoryFolderServiceInterface.Add(InventoryFolder folder)
        {
            if (!IsParentFolderIdValid(folder.Owner.ID, folder.ParentFolderID))
            {
                throw new InvalidParentFolderIdException(string.Format("Invalid parent folder {0} for folder {1}", folder.ParentFolderID, folder.ID));
            }

            m_Folders[folder.Owner.ID].Add(folder.ID, new InventoryFolder(folder));

            if (folder.ParentFolderID != UUID.Zero)
            {
                IncrementVersionNoExcept(folder.Owner.ID, folder.ParentFolderID);
            }
        }

        void IInventoryFolderServiceInterface.Update(InventoryFolder folder)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder internFolder;
            if(m_Folders.TryGetValue(folder.Owner.ID, out folderSet) &&
                folderSet.TryGetValue(folder.ID, out internFolder))
            {
                lock(internFolder)
                {
                    internFolder.Version = folder.Version;
                    internFolder.Name = folder.Name;
                }
                IncrementVersionNoExcept(folder.Owner.ID, folder.ParentFolderID);
            }
        }

        InventoryTree IInventoryFolderServiceInterface.Copy(UUID principalID, UUID folderID, UUID toFolderID) =>
            CopyFolder(principalID, folderID, toFolderID);

        void IInventoryFolderServiceInterface.Move(UUID principalID, UUID folderID, UUID toFolderID)
        {
            if (folderID == toFolderID)
            {
                throw new ArgumentException("folderID != toFolderID");
            }

            if (!IsParentFolderIdValid(principalID, toFolderID, folderID))
            {
                throw new InvalidParentFolderIdException(string.Format("Invalid parent folder {0} for folder {1}", toFolderID, folderID));
            }

            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder internFolder;
            if (m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out internFolder))
            {
                UUID oldFolderID = internFolder.ParentFolderID;
                lock (internFolder)
                {
                    internFolder.ParentFolderID = toFolderID;
                }
                IncrementVersionNoExcept(principalID, toFolderID);
                IncrementVersionNoExcept(principalID, oldFolderID);
                return;
            }
            throw new InventoryFolderNotStoredException(folderID);
        }

        #region Delete and Purge
        void IInventoryFolderServiceInterface.Delete(UUID principalID, UUID folderID)
        {
            PurgeOrDelete(principalID, folderID, true);
        }

        void IInventoryFolderServiceInterface.Purge(UUID principalID, UUID folderID)
        {
            PurgeOrDelete(principalID, folderID, false);
        }

        void IInventoryFolderServiceInterface.Purge(UUID folderID)
        {
            InventoryFolder folder = Folder[folderID];
            Folder.Purge(folder.Owner.ID, folderID);
        }

        private void PurgeOrDelete(UUID principalID, UUID folderID, bool deleteFolder)
        {
            List<UUID> folders;
            InventoryFolder ownfolder;
            if(!Folder.TryGetValue(principalID, folderID, out ownfolder))
            {
                throw new KeyNotFoundException();
            }

            if (deleteFolder)
            {
                folders = new List<UUID>
                {
                    folderID
                };
            }
            else
            {
                folders = GetFolderIDs(principalID, folderID);
            }

            int index = 0;
            while(index < folders.Count)
            {
                foreach (UUID folder in GetFolderIDs(principalID, folders[index]))
                {
                    if (!folders.Contains(folder))
                    {
                        folders.Add(folder);
                    }
                }
                ++index;
            }

            RwLockedDictionary<UUID, InventoryItem> itemSet = m_Items[principalID];
            foreach(InventoryItem item in itemSet.Values)
            {
                if(folders.Contains(item.ParentFolderID))
                {
                    itemSet.Remove(item.ID);
                }
            }

            UUID[] folderArray = folders.ToArray();
            Array.Reverse(folderArray);
            RwLockedDictionary<UUID, InventoryFolder> folderSet = m_Folders[principalID];
            foreach (UUID folder in folderArray)
            {
                folderSet.Remove(folder);
            }

            if (deleteFolder)
            {
                IncrementVersionNoExcept(principalID, ownfolder.ParentFolderID);
            }
            else
            {
                IncrementVersionNoExcept(principalID, folderID);
            }
        }

        private List<UUID> GetFolderIDs(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Folders.TryGetValue(principalID, out folderSet) ?
                new List<UUID>(from folder in folderSet.Values where folder.ParentFolderID == key select folder.ID) :
                new List<UUID>();
        }

        #endregion

        void IInventoryFolderServiceInterface.IncrementVersion(UUID principalID, UUID folderID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder folder;
            if(m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out folder))
            {
                Interlocked.Increment(ref folder.Version);
            }
            else
            {
                throw new InventoryFolderNotStoredException(folderID);
            }
        }

        private void IncrementVersionNoExcept(UUID principalID, UUID folderID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder folder;
            if (m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out folder))
            {
                Interlocked.Increment(ref folder.Version);
            }
        }

        List<UUID> IInventoryFolderServiceInterface.Delete(UUID principalID, List<UUID> folderIDs)
        {
            var deleted = new List<UUID>();
            foreach (var id in folderIDs)
            {
                try
                {
                    Folder.Delete(principalID, id);
                    deleted.Add(id);
                }
                catch
                {
                    /* nothing to do here */
                }
            }

            return deleted;
        }
    }
}
