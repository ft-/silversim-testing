/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.BackendConnectors.IWC.Common;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.IWC.Inventory
{
    class IWCInventoryFolderConnector : InventoryFolderServiceInterface
    {
        private string m_InventoryURI;
        public int TimeoutMs = 20000;
        private GroupsServiceInterface m_GroupsService;

        #region Constructor
        public IWCInventoryFolderConnector(string uri, GroupsServiceInterface groupsService)
        {
            m_GroupsService = groupsService;
            m_InventoryURI = uri;
        }
        #endregion

        #region Accessors
        public override InventoryFolder this[UUID PrincipalID, UUID key]
        {
            get
            {
                InventoryFolder dummy = new InventoryFolder();
                dummy.ID = key;
                dummy.Owner.ID = PrincipalID;

                Map param = new Map
                {
                    {"folder", dummy.FolderToIWC()}
                };

                Map m = IWCGrid.PostToService(m_InventoryURI, "GetFolder", param, TimeoutMs);
                if (m.ContainsKey("Value"))
                {
                    return m.IWCtoFolder();
                }

                throw new InventoryInaccessible();
            }
        }

        public override InventoryFolder this[UUID PrincipalID, InventoryType invtype]
        {
            get
            {
                AssetType type;
                switch(invtype)
                {
                    case InventoryType.Animation: type = AssetType.Animation; break;
                    case InventoryType.Attachable: type = AssetType.Object; break;
                    case InventoryType.Bodypart: type = AssetType.Bodypart; break;
                    case InventoryType.CallingCard: type = AssetType.CallingCard; break;
                    case InventoryType.Clothing: type = AssetType.Clothing; break;
                    case InventoryType.CurrentOutfitFolder: type = AssetType.CurrentOutfitFolder; break;
                    case InventoryType.FavoriteFolder: type = AssetType.FavoriteFolder; break;
                    case InventoryType.Folder: type = AssetType.Folder; break;
                    case InventoryType.Gesture: type = AssetType.Gesture; break;
                    case InventoryType.Inbox: type = AssetType.Inbox; break;
                    case InventoryType.Landmark: type = AssetType.Landmark; break;
                    case InventoryType.LostAndFoundFolder: type = AssetType.LostAndFoundFolder; break;
                    case InventoryType.LSLBytecode: type = AssetType.LSLBytecode; break;
                    case InventoryType.LSLText: type = AssetType.LSLText; break;
                    case InventoryType.Mesh: type = AssetType.Mesh; break;
                    case InventoryType.MyOutfitsFolder: type = AssetType.MyOutfitsFolder; break;
                    case InventoryType.Notecard: type = AssetType.Notecard; break;
                    case InventoryType.Object: type = AssetType.Object; break;
                    case InventoryType.Outbox: type = AssetType.Outbox; break;
                    case InventoryType.OutfitFolder: type = AssetType.OutfitFolder; break;
                    case InventoryType.RootFolder: type = AssetType.RootFolder; break;
                    case InventoryType.Simstate: type = AssetType.Simstate; break;
                    case InventoryType.Snapshot: type = AssetType.SnapshotFolder; break;
                    case InventoryType.Sound: type = AssetType.Sound; break;
                    case InventoryType.Texture: type = AssetType.Texture; break;
                    case InventoryType.TextureTGA: type = AssetType.TextureTGA; break;
                    case InventoryType.TrashFolder: type = AssetType.TrashFolder; break;
                    case InventoryType.Wearable: type = AssetType.Unknown; break;
                    default: type = AssetType.Unknown; break;
                }

                Map param = new Map
                {
                    {"userID", PrincipalID},
                    {"invType", (int)invtype},
                    {"type", (int)type}
                };

                Map m = IWCGrid.PostToService(m_InventoryURI, "GetFolderForType", param, TimeoutMs);
                if (m.ContainsKey("Value"))
                {
                    return m.IWCtoFolder();
                }

                throw new InventoryInaccessible();
            }
        }

        public override List<InventoryFolder> getFolders(UUID PrincipalID, UUID key)
        {
            Map param = new Map
                {
                    {"userID", PrincipalID},
                    {"folderID", key}
                };

            Map m = IWCGrid.PostToService(m_InventoryURI, "GetFolderFolders", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                List<InventoryFolder> folders = new List<InventoryFolder>();
                foreach (IValue v in (AnArray)m["Value"])
                {
                    folders.Add(((Map)v).IWCtoFolder());
                }
                return folders;
            }

            throw new InventoryInaccessible();
        }

        public override List<InventoryItem> getItems(UUID PrincipalID, UUID key)
        {
            Map param = new Map
                {
                    {"userID", PrincipalID},
                    {"folderID", key}
                };

            Map m = IWCGrid.PostToService(m_InventoryURI, "GetFolderItems", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                List<InventoryItem> items = new List<InventoryItem>();
                foreach (IValue v in (AnArray)m["Value"])
                {
                    items.Add(((Map)v).IWCtoItem());
                }
                return items;
            }

            throw new InventoryInaccessible();
        }

        #endregion

        #region Methods

        public override void Add(UUID PrincipalID, InventoryFolder folder)
        {
            Map param = new Map
            {
                {"folder", folder.FolderToIWC()}
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "AddFolder", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryFolderNotStored(folder.ID);
        }
        public override void Update(UUID PrincipalID, InventoryFolder folder)
        {
            Map param = new Map
            {
                {"folder", folder.FolderToIWC()}
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "UpdateFolder", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryFolderNotStored(folder.ID);
        }

        public override void Move(UUID PrincipalID, UUID folderID, UUID toFolderID)
        {
            InventoryFolder folder = this[PrincipalID, folderID];
            folder.ParentFolderID = toFolderID;
            Map param = new Map
            {
                {"folder", folder.FolderToIWC()}
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "MoveFolder", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryFolderNotStored(folder.ID);
        }

        public override void Delete(UUID PrincipalID, UUID folderID)
        {
            Map param = new Map
            {
                {"userID", PrincipalID},
                {"folderIDs", new AnArray { folderID } }
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "DeleteFolders", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryFolderNotStored(folderID);
        }

        public override void Purge(UUID PrincipalID, UUID folderID)
        {
            InventoryFolder folder = this[PrincipalID, folderID];
            Map param = new Map
            {
                {"folder", folder.FolderToIWC()}
            };
            Map m = IWCGrid.PostToService(m_InventoryURI, "PurgeFolder", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if (m["Value"].AsBoolean)
                {
                    return;
                }
            }
            throw new InventoryFolderNotStored(folder.ID);
        }
        #endregion
    }
}
