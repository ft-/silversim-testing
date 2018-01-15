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

using log4net;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace SilverSim.UserCaps.FetchInventoryDescendents2Base
{
    public class FetchInventoryDescendents2Base : FetchInventoryCommon
    {
        private static readonly ILog m_Log = LogManager.GetLogger("FETCHINVENTORYDESCENDENTS2");

        private static void WriteInventoryFolderContent(XmlTextWriter writer, InventoryFolderContent folder,
            bool fetch_folders,
            bool fetch_items, UUID agentID)
        {
            writer.WriteStartElement("map");
            writer.WriteKeyValuePair("agent_id", folder.Owner.ID);
            writer.WriteKeyValuePair("descendents", folder.Folders.Count + folder.Items.Count);
            writer.WriteKeyValuePair("folder_id", folder.FolderID);
            if (fetch_folders)
            {
                writer.WriteStartElement("key");
                writer.WriteValue("categories");
                writer.WriteEndElement();
                writer.WriteStartElement("array");
                foreach (var childfolder in folder.Folders)
                {
                    writer.WriteStartElement("map");
                    writer.WriteKeyValuePair("folder_id", childfolder.ID);
                    writer.WriteKeyValuePair("parent_id", childfolder.ParentFolderID);
                    writer.WriteKeyValuePair("name", childfolder.Name);
                    if (childfolder.DefaultType != AssetType.RootFolder)
                    {
                        writer.WriteKeyValuePair("type", (byte)childfolder.DefaultType);
                    }
                    else
                    {
                        writer.WriteKeyValuePair("type", -1);
                    }
                    writer.WriteKeyValuePair("preferred_type", -1);
                    writer.WriteKeyValuePair("version", childfolder.Version);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            if (fetch_items)
            {
                writer.WriteStartElement("key");
                writer.WriteValue("items");
                writer.WriteEndElement();
                writer.WriteStartElement("array");
                foreach (var childitem in folder.Items)
                {
                    writer.WriteStartElement("map");
                    WriteInventoryItem(childitem, writer, agentID);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteKeyValuePair("owner_id", folder.Owner.ID);
            writer.WriteKeyValuePair("version", folder.Version);
            writer.WriteEndElement();
        }

        protected static void HandleHttpRequest(HttpRequest httpreq, InventoryServiceInterface inventoryService, UUID agentID, UUID requestingOwnerID)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LlsdXml.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }

            var reqmap = o as Map;
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            using (var res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (var text = res.GetOutputStream().UTF8XmlTextWriter())
                {
                    var badfolders = new Dictionary<UUID, string>();
                    text.WriteStartElement("llsd");
                    text.WriteStartElement("map");
                    var wroteheader = false;

                    var folderRequests = new Dictionary<UUID, List<Map>>();

                    var foldersreqarray = (AnArray)reqmap["folders"];
                    foreach (var iv1 in foldersreqarray)
                    {
                        var itemmap = iv1 as Map;
                        if (itemmap == null)
                        {
                            continue;
                        }

                        if (!itemmap.ContainsKey("folder_id") ||
                            !itemmap.ContainsKey("fetch_folders") ||
                            !itemmap.ContainsKey("fetch_items"))
                        {
                            continue;
                        }
                        var ownerid = itemmap["owner_id"].AsUUID;
                        if (!folderRequests.ContainsKey(ownerid))
                        {
                            folderRequests[ownerid] = new List<Map>();
                        }
                        folderRequests[ownerid].Add(itemmap);
                    }

                    var folderContents = new Dictionary<UUID, Dictionary<UUID, InventoryFolderContent>>();
                    foreach (var req in folderRequests)
                    {
                        var list = new List<UUID>();
                        foreach (var fm in req.Value)
                        {
                            if (fm["folder_id"].AsUUID != UUID.Zero)
                            {
                                list.Add(fm["folder_id"].AsUUID);
                            }
                        }
                        try
                        {
                            foreach (var folderContent in inventoryService.Folder.Content[requestingOwnerID, list.ToArray()])
                            {
                                if (!folderContents.ContainsKey(req.Key))
                                {
                                    folderContents.Add(req.Key, new Dictionary<UUID, InventoryFolderContent>());
                                }
                                folderContents[req.Key][folderContent.FolderID] = folderContent;
                            }
                        }
                        catch
                        {
                            /* no action required */
                        }
                    }

                    foreach (var iv in foldersreqarray)
                    {
                        var itemmap = iv as Map;
                        if (iv == null)
                        {
                            continue;
                        }

                        if (!itemmap.ContainsKey("folder_id") ||
                            !itemmap.ContainsKey("fetch_folders") ||
                            !itemmap.ContainsKey("fetch_items"))
                        {
                            continue;
                        }

                        var folderid = itemmap["folder_id"].AsUUID;
                        var ownerid = itemmap["owner_id"].AsUUID;
                        bool fetch_folders = itemmap["fetch_folders"].AsBoolean;
                        bool fetch_items = itemmap["fetch_items"].AsBoolean;

                        if (folderContents.ContainsKey(ownerid))
                        {
                            if (folderContents[ownerid].ContainsKey(folderid))
                            {
                                var fc = folderContents[ownerid][folderid];
                                if (!wroteheader)
                                {
                                    wroteheader = true;
                                    text.WriteNamedValue("key", "folders");
                                    text.WriteStartElement("array");
                                }

                                WriteInventoryFolderContent(text, fc, fetch_folders, fetch_items, agentID);
                            }
                            else
                            {
                                badfolders.Add(folderid, "Not found");
                            }
                        }
                        else
                        {
                            badfolders.Add(folderid, "Not found");
                        }
                    }
                    if (wroteheader)
                    {
                        text.WriteEndElement();
                    }
                    if (badfolders.Count != 0)
                    {
                        text.WriteNamedValue("key", "bad_folders");
                        text.WriteStartElement("array");
                        foreach (KeyValuePair<UUID, string> id in badfolders)
                        {
                            text.WriteStartElement("map");
                            text.WriteNamedValue("folder_id", id.Key);
                            text.WriteNamedValue("error", id.Value);
                            text.WriteEndElement();
                        }
                        text.WriteEndElement();
                    }
                    text.WriteEndElement();
                    text.WriteEndElement();
                }
            }
        }
    }
}
