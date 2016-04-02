// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public class DefaultInventoryFolderContentService : IInventoryFolderContentServiceInterface
    {
        readonly IInventoryFolderServiceInterface m_Service;

        public DefaultInventoryFolderContentService(IInventoryFolderServiceInterface service)
        {
            m_Service = service;
        }

        public bool TryGetValue(UUID principalID, UUID folderID, out InventoryFolderContent inventoryFolderContent)
        {
            try
            {
                inventoryFolderContent = this[principalID, folderID];
                return true;
            }
            catch
            {
                inventoryFolderContent = null;
                return false;
            }
        }

        public bool ContainsKey(UUID principalID, UUID folderID)
        {
            return m_Service.ContainsKey(principalID, folderID);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public List<InventoryFolderContent> this[UUID principalID, UUID[] folderIDs]
        {
            get
            {
                List<InventoryFolderContent> res = new List<InventoryFolderContent>();
                foreach(UUID folder in folderIDs)
                {
                    try
                    {
                        res.Add(this[principalID, folder]);
                    }
                    catch
                    {
                        /* nothing that we should do here */
                    }
                }

                return res;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public InventoryFolderContent this[UUID principalID, UUID folderID]
        {
            get 
            {
                InventoryFolderContent folderContent = new InventoryFolderContent();
                InventoryFolder folder;
                folder = m_Service[principalID, folderID];

                folderContent.Version = folder.Version;
                folderContent.Owner = folder.Owner;
                folderContent.FolderID = folder.ID;

                try
                {
                    folderContent.Folders = m_Service.GetFolders(principalID, folderID);
                }
                catch
                {
                    folderContent.Folders = new List<InventoryFolder>();
                }

                try
                {
                    folderContent.Items = m_Service.GetItems(principalID, folderID);
                }
                catch
                {
                    folderContent.Items = new List<InventoryItem>();
                }

                return folderContent;
            }
        }
    }
}
