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

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using HttpClasses;
using System.Collections.Generic;
using SilverSim.BackendConnectors.Simian.Common;
using System;

namespace SilverSim.BackendConnectors.Simian.Inventory
{
    class SimianInventoryFolderConnector : InventoryFolderServiceInterface
    {
        private string m_InventoryURI;
        public int TimeoutMs = 20000;
        private GroupsServiceInterface m_GroupsService;
        private string m_SimCapability;

        #region Constructor
        public SimianInventoryFolderConnector(string uri, GroupsServiceInterface groupsService, string simCapability)
        {
            m_SimCapability = simCapability;
            m_GroupsService = groupsService;
            m_InventoryURI = uri;
        }
        #endregion

        #region Accessors
        public override InventoryFolder this[UUID PrincipalID, UUID key]
        {
            get
            {
                throw new NotImplementedException();
#if NOT_IMPLEMENTED
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["PRINCIPAL"] = PrincipalID;
                post["ID"] = key;
                post["METHOD"] = "GETFOLDER";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
                if(!map.ContainsKey("folder"))
                {
                    throw new InventoryInaccessible();
                }
                else if (!(map["folder"] is Map))
                {
                    throw new InventoryInaccessible();
                }

                return RobustInventoryConnector.FolderFromMap((Map)map["folder"]);
#endif
            }
        }

        public override InventoryFolder this[UUID PrincipalID, InventoryType type]
        {
            get
            {
                throw new NotImplementedException();
#if NOT_IMPLEMENTED
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["PRINCIPAL"] = PrincipalID;
                if (type == InventoryType.RootFolder)
                {
                    post["METHOD"] = "GETROOTFOLDER";
                }
                else
                { 
                    post["TYPE"] = ((int)type).ToString();
                    post["METHOD"] = "GETFOLDERFORTYPE";
                }
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
                if (!(map["folder"] is Map))
                {
                    throw new InventoryInaccessible();
                }

                return RobustInventoryConnector.FolderFromMap((Map)map["folder"]);
#endif
            }
        }

        public override List<InventoryFolder> getFolders(UUID PrincipalID, UUID key)
        {
                throw new NotImplementedException();
#if NOT_IMPLEMENTED
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["PRINCIPAL"] = PrincipalID;
            post["FOLDER"] = key;
            post["METHOD"] = "GETFOLDERCONTENT";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!(map["FOLDERS"] is Map))
            {
                throw new InventoryInaccessible();
            }

            List<InventoryFolder> items = new List<InventoryFolder>();
            foreach (KeyValuePair<string, IValue> i in (Map)map["FOLDERS"])
            {
                if (i.Value is Map)
                {
                    items.Add(RobustInventoryConnector.FolderFromMap((Map)i.Value));
                }
            }
            return items;
#endif
        }

        public override List<InventoryItem> getItems(UUID PrincipalID, UUID key)
        {
                throw new NotImplementedException();
#if NOT_IMPLEMENTED
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["PRINCIPAL"] = PrincipalID;
            post["FOLDER"] = key;
            post["METHOD"] = "GETFOLDERITEMS";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!(map["ITEMS"] is Map))
            {
                throw new InventoryInaccessible();
            }

            List<InventoryItem> items = new List<InventoryItem>();
            foreach (KeyValuePair<string, IValue> i in (Map)map["ITEMS"])
            {
                if (i.Value is Map)
                {
                    items.Add(RobustInventoryConnector.ItemFromMap((Map)i.Value, m_GroupsService));
                }
            }
            return items;
#endif
        }

        public override List<InventoryFolder> getSkeleton(UUID PrincipalID)
        {
                throw new NotImplementedException();
#if NOT_IMPLEMENTED
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["PRINCIPAL"] = PrincipalID;
            post["METHOD"] = "GETINVENTORYSKELETON";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!(map["FOLDERS"] is Map))
            {
                throw new InventoryInaccessible();
            }

            List<InventoryFolder> items = new List<InventoryFolder>();
            foreach (KeyValuePair<string, IValue> i in (Map)map["FOLDERS"])
            {
                if (i.Value is Map)
                {
                    items.Add(RobustInventoryConnector.FolderFromMap((Map)i.Value));
                }
            }
            return items;
#endif
        }

        #endregion

        #region Methods

        public override void Add(UUID PrincipalID, InventoryFolder folder)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RequestMethod"] = "AddInventoryFolder";
            post["FolderID"] = folder.ID;
            post["ParentID"] = folder.ParentFolderID;
            post["ContentType"] = ((int)folder.InventoryType).ToString();
            post["Name"] = folder.Name;
            post["OwnerID"] = folder.Owner.ID;

            Map m = SimianGrid.PostToService(m_InventoryURI, m_SimCapability, post, TimeoutMs);
            if (!m["Success"].AsBoolean)
            {
                throw new InventoryFolderNotStored(folder.ID);
            }
        }
        public override void Update(UUID PrincipalID, InventoryFolder folder)
        {
            Add(PrincipalID, folder);
        }

        public override void Move(UUID PrincipalID, UUID folderID, UUID toFolderID)
        {
                throw new NotImplementedException();
#if NOT_IMPLEMENTED
            Dictionary<string, string> post = new Dictionary<string,string>();
            post["ParentID"] = toFolderID;
            post["ID"] = folderID;
            post["PRINCIPAL"] = PrincipalID;
            post["METHOD"] = "MOVEFOLDER";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!((AString)map["RESULT"]))
            {
                throw new InventoryFolderNotStored(folderID);
            }
#endif
        }

        public override void Delete(UUID PrincipalID, UUID folderID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RequestMethod"] = "RemoveInventoryNode";
            post["OwnerID"] = PrincipalID;
            post["ItemID"] = folderID;

            Map m = SimianGrid.PostToService(m_InventoryURI, m_SimCapability, post, TimeoutMs);
            if(!m["Success"].AsBoolean)
            {
                throw new InventoryFolderNotStored(folderID);
            }
        }

        public override void Purge(UUID PrincipalID, UUID folderID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["RequestMethod"] = "PurgeInventoryFolder";
            post["OwnerID"] = PrincipalID;
            post["FolderID"] = folderID;

            Map m = SimianGrid.PostToService(m_InventoryURI, m_SimCapability, post, TimeoutMs);
            if(!m["Success"].AsBoolean)
            {
                throw new InventoryFolderNotStored(folderID);
            }
        }
        #endregion
    }
}
