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
using SilverSim.Types.Inventory;
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

        public override InventoryFolder this[UUID PrincipalID, InventoryType type]
        {
            get
            {
#if NOT_IMPLEMENTED
                Dictionary<string, string> post = new Dictionary<string, string>();
                if (type == InventoryType.RootFolder)
                {
                    post["RequestMethod"] = "GetInventoryNode";
                    post["ItemID"] = PrincipalID;
                    post["OwnerID"] = PrincipalID;
                    post["IncludeFolders"] = "1";
                    post["IncludeItems"] = "0";
                    post["ChildrenOnly"] = "1";
                }
                else
                {
                    post["RequestMethod"] = "GetFolderForType";
                    post["OwnerID"] = PrincipalID;
                    post["ContentType"] = SimianInventoryConnector.ContentTypeFromInventoryType(type);
                }
                Map res = SimianGrid.PostToService(m_InventoryURI, m_SimCapability, post, TimeoutMs);
                if (res["Success"].AsBoolean && res.ContainsKey("Items") && res["Items"] is AnArray)
                {
                    Map m = (Map)((AnArray)res["Items"])[0];
                    return SimianInventoryConnector.FolderFromMap(m);
                }
#endif
                throw new InventoryInaccessible();
            }
        }

        public override List<InventoryFolder> getFolders(UUID PrincipalID, UUID key)
        {
#if NOT_IMPLEMENTED
            List<InventoryFolder> folders = new List<InventoryFolder>();
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RequestMethod"] = "GetInventoryNode";
            post["ItemID"] = key;
            post["OwnerID"] = PrincipalID;
            post["IncludeFolders"] = "1";
            post["IncludeItems"] = "0";
            post["ChildrenOnly"] = "1";

            Map res = SimianGrid.PostToService(m_InventoryURI, m_SimCapability, post, TimeoutMs);
            if (res["Success"].AsBoolean && res.ContainsKey("Items") && res["Items"] is AnArray)
            {
                foreach (IValue iv in (AnArray)res["Items"])
                {
                    if (iv is Map)
                    {
                        Map m = (Map)iv;
                        if (m["Type"].ToString() == "Folder")
                        {
                            folders.Add(SimianInventoryConnector.FolderFromMap(m));
                        }
                    }
                }
                return folders;
            }
#endif
            throw new InventoryInaccessible();
        }

        public override List<InventoryItem> getItems(UUID PrincipalID, UUID key)
        {
#if NOT_IMPLEMENTED

            List<InventoryItem> items = new List<InventoryItem>();
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RequestMethod"] = "GetInventoryNode";
            post["ItemID"] = key;
            post["OwnerID"] = PrincipalID;
            post["IncludeFolders"] = "0";
            post["IncludeItems"] = "1";
            post["ChildrenOnly"] = "1";

            Map res = SimianGrid.PostToService(m_InventoryURI, m_SimCapability, post, TimeoutMs);
            if (res["Success"].AsBoolean && res.ContainsKey("Items") && res["Items"] is AnArray)
            {
                foreach (IValue iv in (AnArray)res["Items"])
                {
                    if (iv is Map)
                    {
                        Map m = (Map)iv;
                        if (m["Type"].ToString() == "Item")
                        {
                            items.Add(SimianInventoryConnector.ItemFromMap(m, m_GroupsService));
                        }
                    }
                }
                return items;
            }
#endif
            throw new InventoryInaccessible();
        }

        public override List<InventoryFolder> getSkeleton(UUID PrincipalID)
        {
#if NOT_IMPLEMENTED
            List<InventoryFolder> folders = new List<InventoryFolder>();
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RequestMethod"] = "GetInventoryNode";
            post["ItemID"] = PrincipalID;
            post["OwnerID"] = PrincipalID;
            post["IncludeFolders"] = "1";
            post["IncludeItems"] = "0";
            post["ChildrenOnly"] = "0";

            Map res = SimianGrid.PostToService(m_InventoryURI, m_SimCapability, post, TimeoutMs);
            if (res["Success"].AsBoolean && res.ContainsKey("Items") && res["Items"] is AnArray)
            {
                foreach (IValue iv in (AnArray)res["Items"])
                {
                    if (iv is Map)
                    {
                        Map m = (Map)iv;
                        if (m["Type"].ToString() == "Folder")
                        {
                            folders.Add(SimianInventoryConnector.FolderFromMap(m));
                        }
                    }
                }
                return folders;
            }
#endif
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
