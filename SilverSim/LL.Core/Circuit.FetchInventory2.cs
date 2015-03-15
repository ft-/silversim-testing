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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System.Net;
using System.Xml;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types.Inventory;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void WriteInventoryItem(InventoryItem item, XmlTextWriter writer)
        {
            writer.WriteKeyValuePair("asset_id", item.AssetID);
            writer.WriteKeyValuePair("created_at", (uint)item.CreationDate.DateTimeToUnixTime());
            writer.WriteKeyValuePair("desc", item.Description);
            writer.WriteKeyValuePair("flags", item.Flags);
            writer.WriteKeyValuePair("item_id", item.ID);
            writer.WriteKeyValuePair("name", item.Name);
            writer.WriteKeyValuePair("parent_id", item.ParentFolderID);
            writer.WriteKeyValuePair("type", (uint)item.AssetType);
            writer.WriteKeyValuePair("inv_type", (uint)item.InventoryType);

            writer.WriteStartElement("key");
            writer.WriteValue("permissions");
            writer.WriteEndElement();
            writer.WriteStartElement("map");
            uint basePermissions = (uint)item.Permissions.Base;
            if (AgentID == item.Creator.ID)
            {
                basePermissions |= (uint)InventoryPermissionsMask.Transfer | (uint)InventoryPermissionsMask.Copy | (uint)InventoryPermissionsMask.Modify;
            }
            if (AgentID == item.Owner.ID)
            {
                basePermissions |= (uint)item.Permissions.Current;
            }
            basePermissions |= (uint)item.Permissions.EveryOne;

            writer.WriteKeyValuePair("base_mask", (uint)basePermissions);
            writer.WriteKeyValuePair("creator_id", item.Creator.ID);
            writer.WriteKeyValuePair("everyone_mask", (uint)item.Permissions.EveryOne);
            writer.WriteKeyValuePair("group_id", item.Group.ID);
            writer.WriteKeyValuePair("group_mask", (uint)item.Permissions.Group);
            writer.WriteKeyValuePair("is_owner_group", item.IsGroupOwned);
            writer.WriteKeyValuePair("next_owner_mask", (uint)item.Permissions.NextOwner);
            writer.WriteKeyValuePair("owner_id", item.Owner.ID);
            writer.WriteKeyValuePair("owner_mask", (uint)item.Permissions.Current);
            writer.WriteEndElement();

            writer.WriteStartElement("key");
            writer.WriteValue("sale_info");
            writer.WriteEndElement();
            writer.WriteStartElement("map");
            writer.WriteKeyValuePair("sale_price", item.SaleInfo.Price);
            writer.WriteKeyValuePair("sale_type", (uint)item.SaleInfo.Type);
            writer.WriteEndElement();
        }

        void Cap_FetchInventory2(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (!(o is Map))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            Map reqmap = (Map)o;

            HttpResponse res = httpreq.BeginResponse();
            XmlTextWriter text = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM);
            List<UUID> baditems = new List<UUID>();
            text.WriteStartElement("llsd");
            text.WriteStartElement("map");
            text.WriteStartElement("key");
            text.WriteValue("agent_id");
            text.WriteEndElement();
            text.WriteStartElement("uuid");
            text.WriteValue(AgentID);
            text.WriteEndElement();
            bool wroteheader = false;

            foreach(IValue iv in (AnArray)reqmap["items"])
            {
                if(!(iv is Map))
                {
                    continue;
                }

                Map itemmap = (Map) iv;
                if(!itemmap.ContainsKey("item_id"))
                {
                    continue;
                }
                UUID itemid = itemmap["item_id"].AsUUID;
                InventoryItem item;
                try
                {
                    item = Agent.InventoryService.Item[AgentID, itemid];
                }
                catch
                {
                    baditems.Add(itemid);
                    continue;
                }
                if (!wroteheader)
                {
                    wroteheader = true;
                    text.WriteStartElement("key");
                    text.WriteValue("items");
                    text.WriteEndElement();
                    text.WriteStartElement("array");
                }
                WriteInventoryItem(item, text);
            }
            if(wroteheader)
            {
                text.WriteEndElement();
            }
            if(baditems.Count != 0)
            {
                text.WriteStartElement("key");
                text.WriteValue("bad_items");
                text.WriteEndElement();
                text.WriteStartElement("array");
                foreach(UUID id in baditems)
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
