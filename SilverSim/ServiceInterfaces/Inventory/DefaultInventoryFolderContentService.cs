// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public class DefaultInventoryFolderContentService : InventoryFolderContentServiceInterface
    {
        InventoryFolderServiceInterface m_Service;

        public DefaultInventoryFolderContentService(InventoryFolderServiceInterface service)
        {
            m_Service = service;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override InventoryFolderContent this[UUID principalID, UUID folderID]
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
