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

using SilverSim.BackendConnectors.Robust.Common;
using SilverSim.Main.Common.HttpClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.Inventory
{
    public class RobustInventoryFolderContentConnector : InventoryFolderContentServiceInterface
    {
        bool m_IsMultipeServiceSupported = true;
        string m_InventoryURI;
        public int TimeoutMs = 20000;
        private GroupsServiceInterface m_GroupsService;

        public RobustInventoryFolderContentConnector(string url, GroupsServiceInterface groupsService)
        {
            m_InventoryURI = url;
            m_GroupsService = groupsService;
        }

        #region Private duplicate (keeps InventoryFolderConnector from having a circular reference)
        InventoryFolder GetFolder(UUID PrincipalID, UUID key)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["PRINCIPAL"] = PrincipalID;
            post["ID"] = key;
            post["METHOD"] = "GETFOLDER";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!map.ContainsKey("folder"))
            {
                throw new InventoryInaccessible();
            }
            else if (!(map["folder"] is Map))
            {
                throw new InventoryInaccessible();
            }

            return RobustInventoryConnector.FolderFromMap((Map)map["folder"]);
        }
        #endregion

        public override InventoryFolderContent this[UUID principalID, UUID folderID]
        {
            get 
            {
                InventoryFolderContent folderContent = new InventoryFolderContent();
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["PRINCIPAL"] = principalID;
                post["FOLDER"] = folderID;
                post["METHOD"] = "GETFOLDERCONTENT";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));

                folderContent.Owner.ID = principalID;
                folderContent.FolderID = folderID;
                folderContent.Version = 0;
                if(map.ContainsKey("VERSION"))
                {
                    folderContent.Version = map["VERSION"].AsInt;
                }
                else
                {
                    InventoryFolder folder = GetFolder(principalID, folderID);
                    folderContent.Version = folder.Version;
                }

                if (map.ContainsKey("FOLDERS") && map["FOLDERS"] is Map)
                {
                    foreach (KeyValuePair<string, IValue> i in (Map)map["FOLDERS"])
                    {
                        if (i.Value is Map)
                        {
                            folderContent.Folders.Add(RobustInventoryConnector.FolderFromMap((Map)i.Value));
                        }
                    }
                }
                if(map.ContainsKey("ITEMS") && map["ITEMS"] is Map)
                {
                    foreach (KeyValuePair<string, IValue> i in (Map)map["ITEMS"])
                    {
                        if (i.Value is Map)
                        {
                            folderContent.Items.Add(RobustInventoryConnector.ItemFromMap((Map)i.Value, m_GroupsService));
                        }
                    }
                }
                return folderContent;
            }
        }

        public override List<InventoryFolderContent> this[UUID principalID, UUID[] folderIDs]
        {
            get
            {
                if(folderIDs.Length == 0)
                {
                    return new List<InventoryFolderContent>();
                }

                /* when the service failed for being not supported, we do not even try it again in that case */
                if (!m_IsMultipeServiceSupported)
                {
                    return base[principalID, folderIDs];
                }

                Dictionary<string, string> post = new Dictionary<string, string>();
                post["PRINCIPAL"] = principalID;
                post["FOLDERS"] = string.Join(",", folderIDs);
                post["COUNT"] = folderIDs.Length.ToString(); /* <- some redundancy here for whatever unknown reason, it could have been derived from FOLDERS anyways */
                post["METHOD"] = "GETMULTIPLEFOLDERSCONTENT";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));

                List<InventoryFolderContent> contents = new List<InventoryFolderContent>();

                foreach(KeyValuePair<string, IValue> kvp in map)
                {
                    if(kvp.Key.StartsWith("F_") && kvp.Value is Map)
                    {
                        Map fc = (Map)kvp.Value;

                        InventoryFolderContent folderContent = new InventoryFolderContent();
                        folderContent.Owner.ID = fc["OWNER"].AsUUID;
                        folderContent.FolderID = fc["FID"].AsUUID;
                        folderContent.Version = fc["VERSION"].AsInt;

                        if (map.ContainsKey("FOLDERS") && map["FOLDERS"] is Map)
                        {
                            foreach (KeyValuePair<string, IValue> i in (Map)map["FOLDERS"])
                            {
                                if (i.Value is Map)
                                {
                                    folderContent.Folders.Add(RobustInventoryConnector.FolderFromMap((Map)i.Value));
                                }
                            }
                        }

                        if (map.ContainsKey("ITEMS") && map["ITEMS"] is Map)
                        {
                            foreach (KeyValuePair<string, IValue> i in (Map)map["ITEMS"])
                            {
                                if (i.Value is Map)
                                {
                                    folderContent.Items.Add(RobustInventoryConnector.ItemFromMap((Map)i.Value, m_GroupsService));
                                }
                            }
                        }
                    }
                }

                if(contents.Count == 0)
                {
                    /* try old method */
                    contents = base[principalID, folderIDs];
                    if(contents.Count > 0)
                    {
                        m_IsMultipeServiceSupported = false;
                    }
                }
                return contents;
            }
        }

    }
}
