// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.BackendConnectors.IWC.Common;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.BackendConnectors.IWC.Inventory
{
    class IWCInventoryItemConnector : InventoryItemServiceInterface
    {
        private string m_InventoryURI;
        public int TimeoutMs = 20000;
        private GroupsServiceInterface m_GroupsService;

        #region Constructor
        public IWCInventoryItemConnector(string uri, GroupsServiceInterface groupsService)
        {
            m_GroupsService = groupsService;
            m_InventoryURI = uri;
        }
        #endregion

        #region Accessors
        public override InventoryItem this[UUID key]
        {
            get 
            { 
                throw new NotImplementedException(); 
            }
        }
        public override InventoryItem this[UUID PrincipalID, UUID key]
        {
            get
            {
                Map param = new Map
                {
                    {"userID", PrincipalID},
                    {"inventoryID", key}
                };
                Map m = IWCGrid.PostToService(m_InventoryURI, "GetItem", param, TimeoutMs);
                if (m.ContainsKey("Value"))
                {
                    return ((Map)m["Value"]).IWCtoItem();
                }

                throw new InventoryItemNotFound(key);
            }
        }
        #endregion

        public override void Add(InventoryItem item)
        {
            Map param = new Map
            {
                {"item", item.ItemToIWC()},
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "AddItem", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if(m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryItemNotStored(item.ID);
        }

        public override void Update(InventoryItem item)
        {
            Map param = new Map
            {
                {"item", item.ItemToIWC()},
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "UpdateItem", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryItemNotStored(item.ID);
        }

        public override void Delete(UUID PrincipalID, UUID ID)
        {
            Map param = new Map
            {
                {"userID", PrincipalID},
                {"itemIDs", new AnArray { ID } }
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "DeleteItems", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryItemNotFound(ID);
        }

        public override void Move(UUID PrincipalID, UUID ID, UUID newFolder)
        {
            InventoryItem item = this[PrincipalID, ID];
            item.ParentFolderID = newFolder;

            Map param = new Map
            {
                {"ownerID", PrincipalID},
                {"items", new AnArray { item.ItemToIWC() } }
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "MoveItems", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryItemNotStored(ID);
        }
    }
}
