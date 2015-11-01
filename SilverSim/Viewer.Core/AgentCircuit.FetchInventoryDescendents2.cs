// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        void WriteInventoryFolderContent(XmlTextWriter writer, InventoryFolderContent folder, 
            bool fetch_folders,
            bool fetch_items, List<InventoryItem> linkeditems)
        {
            writer.WriteStartElement("map");
            writer.WriteKeyValuePair("agent_id", folder.Owner.ID);
            writer.WriteKeyValuePair("descendents", folder.Folders.Count + folder.Items.Count);
            writer.WriteKeyValuePair("folder_id", folder.FolderID);
            if(fetch_folders)
            {
                writer.WriteStartElement("key");
                writer.WriteValue("categories");
                writer.WriteEndElement();
                writer.WriteStartElement("array");
                foreach(InventoryFolder childfolder in folder.Folders)
                {
                    writer.WriteStartElement("map");
                    writer.WriteKeyValuePair("folder_id", childfolder.ID);
                    writer.WriteKeyValuePair("parent_id", childfolder.ParentFolderID);
                    writer.WriteKeyValuePair("name", childfolder.Name);
                    if (childfolder.InventoryType != InventoryType.Folder)
                    {
                        writer.WriteKeyValuePair("type", (byte)childfolder.InventoryType);
                    }
                    else
                    {
                        writer.WriteKeyValuePair("type", -1);
                    }
                    writer.WriteKeyValuePair("preferred_type", -1);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            if(fetch_items)
            {
                writer.WriteStartElement("key");
                writer.WriteValue("items");
                writer.WriteEndElement();
                writer.WriteStartElement("array");
                if(linkeditems != null)
                {
                    foreach (InventoryItem childitem in linkeditems)
                    {
                        writer.WriteStartElement("map");
                        WriteInventoryItem(childitem, writer);
                        writer.WriteEndElement();
                    }
                }
                foreach (InventoryItem childitem in folder.Items)
                {
                    writer.WriteStartElement("map");
                    WriteInventoryItem(childitem, writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteKeyValuePair("owner_id", folder.Owner.ID);
            writer.WriteKeyValuePair("version", folder.Version);
            writer.WriteEndElement();
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        void Cap_FetchInventoryDescendents2(HttpRequest httpreq)
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

            Map reqmap = o as Map;
            if (null == reqmap)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            using (HttpResponse res = httpreq.BeginResponse())
            {
                using (XmlTextWriter text = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM))
                {
                    List<UUID> badfolders = new List<UUID>();
                    text.WriteStartElement("llsd");
                    text.WriteStartElement("map");
                    bool wroteheader = false;

                    Dictionary<UUID, List<Map>> folderRequests = new Dictionary<UUID, List<Map>>();

                    AnArray foldersreqarray = (AnArray)reqmap["folders"];
                    foreach (IValue iv1 in foldersreqarray)
                    {
                        Map itemmap = iv1 as Map;
                        if (null == itemmap)
                        {
                            continue;
                        }

                        if (!itemmap.ContainsKey("folder_id") ||
                            !itemmap.ContainsKey("fetch_folders") ||
                            !itemmap.ContainsKey("fetch_items"))
                        {
                            continue;
                        }
                        UUID ownerid = itemmap["owner_id"].AsUUID;
                        if (!folderRequests.ContainsKey(ownerid))
                        {
                            folderRequests[ownerid] = new List<Map>();
                        }
                        folderRequests[ownerid].Add(itemmap);
                    }

                    Dictionary<UUID, Dictionary<UUID, InventoryFolderContent>> folderContents = new Dictionary<UUID, Dictionary<UUID, InventoryFolderContent>>();
                    foreach (KeyValuePair<UUID, List<Map>> req in folderRequests)
                    {
                        List<UUID> list = new List<UUID>();
                        foreach (Map fm in req.Value)
                        {
                            if (fm["folder_id"].AsUUID != UUID.Zero)
                            {
                                list.Add(fm["folder_id"].AsUUID);
                            }
                        }
                        try
                        {
                            List<InventoryFolderContent> folderContentRes = Agent.InventoryService.Folder.Content[AgentID, list.ToArray()];
                            foreach (InventoryFolderContent folderContent in folderContentRes)
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

                        }
                    }

                    foreach (IValue iv in foldersreqarray)
                    {
                        Map itemmap = iv as Map;
                        if (null == iv)
                        {
                            continue;
                        }

                        if (!itemmap.ContainsKey("folder_id") ||
                            !itemmap.ContainsKey("fetch_folders") ||
                            !itemmap.ContainsKey("fetch_items"))
                        {
                            continue;
                        }

                        UUID folderid = itemmap["folder_id"].AsUUID;
                        UUID ownerid = itemmap["owner_id"].AsUUID;
                        bool fetch_folders = itemmap["fetch_folders"].AsBoolean;
                        bool fetch_items = itemmap["fetch_items"].AsBoolean;


                        if (folderContents.ContainsKey(ownerid))
                        {
                            if (folderContents[ownerid].ContainsKey(folderid))
                            {
                                InventoryFolderContent fc = folderContents[ownerid][folderid];
                                List<InventoryItem> linkeditems = new List<InventoryItem>();
                                List<UUID> linkeditemids = new List<UUID>();

                                foreach (InventoryItem item in fc.Items)
                                {
                                    if (item.AssetType == Types.Asset.AssetType.Link)
                                    {
                                        linkeditemids.Add(item.AssetID);
                                    }
                                    else if (item.AssetType == Types.Asset.AssetType.LinkFolder)
                                    {

                                    }
                                }

                                try
                                {
                                    linkeditems = Agent.InventoryService.Item[ownerid, linkeditemids];
                                }
                                catch
                                {

                                }
                                if (!wroteheader)
                                {
                                    wroteheader = true;
                                    text.WriteNamedValue("key", "folders");
                                    text.WriteStartElement("array");
                                }

                                WriteInventoryFolderContent(text, fc, fetch_folders, fetch_items, linkeditems);
                            }
                            else
                            {
                                badfolders.Add(folderid);
                            }
                        }
                        else
                        {
                            badfolders.Add(folderid);
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
                        foreach (UUID id in badfolders)
                        {
                            text.WriteNamedValue("uuid", id);
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
