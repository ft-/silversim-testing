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

using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void WriteInventoryFolder(XmlTextWriter writer, InventoryFolder folder, 
            bool fetch_folders, List<InventoryFolder> folders,
            bool fetch_items, List<InventoryItem> items, List<InventoryItem> linkeditems)
        {
            writer.WriteStartElement("map");
            WriteKeyValuePair(writer, "agent_id", folder.Owner.ID);
            WriteKeyValuePair(writer, "descendents", folders.Count + items.Count);
            WriteKeyValuePair(writer, "folder_id", folder.ID);
            if(fetch_folders)
            {
                writer.WriteStartElement("key");
                writer.WriteValue("categories");
                writer.WriteEndElement();
                writer.WriteStartElement("array");
                foreach(InventoryFolder childfolder in folders)
                {
                    writer.WriteStartElement("map");
                    WriteKeyValuePair(writer, "folder_id", childfolder.ID);
                    WriteKeyValuePair(writer, "parent_id", childfolder.ParentFolderID);
                    WriteKeyValuePair(writer, "name", childfolder.Name);
                    if (childfolder.InventoryType != InventoryType.Folder)
                    {
                        WriteKeyValuePair(writer, "type", (byte)childfolder.InventoryType);
                    }
                    else
                    {
                        WriteKeyValuePair(writer, "type", -1);
                    }
                    WriteKeyValuePair(writer, "preferred_type", -1);
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
                foreach (InventoryItem childitem in items)
                {
                    writer.WriteStartElement("map");
                    WriteInventoryItem(childitem, writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            WriteKeyValuePair(writer, "owner_id", folder.Owner.ID);
            WriteKeyValuePair(writer, "version", folder.Version);
            writer.WriteEndElement();
        }

        void Cap_FetchInventoryDescendents2(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type").Close();
                return;
            }
            if (!(o is Map))
            {
                httpreq.BeginResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML").Close();
                return;
            }
            Map reqmap = (Map)o;

            HttpResponse res = httpreq.BeginResponse();
            XmlTextWriter text = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM);
            List<UUID> badfolders = new List<UUID>();
            text.WriteStartElement("llsd");
            text.WriteStartElement("map");
            bool wroteheader = false;

            foreach (IValue iv in (AnArray)reqmap["folders"])
            {
                if (!(iv is Map))
                {
                    continue;
                }

                Map itemmap = (Map)iv;
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

                InventoryFolder folder;
                try
                {
                    folder = Agent.InventoryService.Folder[AgentID, folderid];
                }
                catch
                {
                    badfolders.Add(folderid);
                    continue;
                }
                List<InventoryFolder> childfolders;
                List<InventoryItem> childitems;
                List<InventoryItem> linkeditems = new List<InventoryItem>();
                try
                {
                    childfolders = Agent.InventoryService.Folder.getFolders(ownerid, folderid);
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("Inventory.Folder.getFolders failed for {0}/{1}: {2}", ownerid, folderid, e.Message);
                    childfolders = new List<InventoryFolder>();
                }

                try
                {
                    childitems = Agent.InventoryService.Folder.getItems(AgentID, folderid);
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("Inventory.Folder.getItems failed for {0}/{1}: {2}", ownerid, folderid, e.Message);
                    childitems = new List<InventoryItem>();
                }

                foreach(InventoryItem item in childitems)
                {
                    if(item.AssetType == Types.Asset.AssetType.Link)
                    {
                        try
                        {
                            linkeditems.Add(Agent.InventoryService.Item[ownerid, item.AssetID]);
                        }
                        catch
                        {
                            /* item missing */
                        }
                    }
                    else if(item.AssetType == Types.Asset.AssetType.LinkFolder)
                    {

                    }
                }
                if (!wroteheader)
                {
                    wroteheader = true;
                    text.WriteStartElement("key");
                    text.WriteValue("folders");
                    text.WriteEndElement();
                    text.WriteStartElement("array");
                }
                WriteInventoryFolder(text, folder, fetch_folders, childfolders, fetch_items, childitems, linkeditems);
            }
            if (wroteheader)
            {
                text.WriteEndElement();
            }
            if (badfolders.Count != 0)
            {
                text.WriteStartElement("key");
                text.WriteValue("bad_folders");
                text.WriteEndElement();
                text.WriteStartElement("array");
                foreach (UUID id in badfolders)
                {
                    text.WriteStartElement("uuid");
                    text.WriteValue(id);
                    text.WriteEndElement();
                }
                text.WriteEndElement();
            }
            text.WriteEndElement();
            text.WriteEndElement();
            text.Flush();

            res.Close();
        }
    }
}
