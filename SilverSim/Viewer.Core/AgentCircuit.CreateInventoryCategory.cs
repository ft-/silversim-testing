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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Net;
using System.Xml;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private void Cap_CreateInventoryCategory(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden);
                return;
            }
            if(httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed);
                return;
            }

            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(httpreq.Body) as Map;
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType);
                return;
            }
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            var folder = new InventoryFolder(reqmap["folder_id"].AsUUID)
            {
                ParentFolderID = reqmap["parent_id"].AsUUID,
                Name = reqmap["name"].ToString(),
                DefaultType = (AssetType)reqmap["type"].AsInt,
                Owner = Agent.Owner,
                Version = 1
            };

            try
            {
                Agent.InventoryService.Folder.Add(folder);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest);
                return;
            }

            using (HttpResponse httpres = httpreq.BeginResponse("application/llsd+xml"))
            using (XmlTextWriter writer = httpres.GetOutputStream().UTF8XmlTextWriter())
            {
                writer.WriteStartElement("map");
                writer.WriteNamedValue("key", "folder_id");
                writer.WriteNamedValue("uuid", folder.ID);
                writer.WriteNamedValue("key", "name");
                writer.WriteNamedValue("string", folder.Name);
                writer.WriteNamedValue("key", "parent_id");
                writer.WriteNamedValue("uuid", folder.ParentFolderID);
                writer.WriteNamedValue("key", "type");
                writer.WriteNamedValue("integer", (int)folder.DefaultType);
                writer.WriteEndElement();
            }
        }
    }
}
