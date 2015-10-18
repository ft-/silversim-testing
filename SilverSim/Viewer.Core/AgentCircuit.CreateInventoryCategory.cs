// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
using System.IO;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        public void Cap_CreateInventoryCategory(HttpRequest httpreq)
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
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }

            Map reqmap = o as Map;
            if(null == reqmap)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            InventoryFolder folder = new InventoryFolder();
            folder.ID = reqmap["folder_id"].AsUUID;
            folder.ParentFolderID = reqmap["parent_id"].AsUUID;
            folder.InventoryType = (InventoryType)reqmap["type"].AsInt;
            folder.Name = reqmap["name"].ToString();
            folder.Version = 1;

            try
            {
                Agent.InventoryService.Folder.Add(folder);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
                return;
            }

            Map resmap = new Map();
            resmap.Add("folder_id", folder.ID);
            resmap.Add("parent_id", folder.ParentFolderID);
            resmap.Add("type", (int)folder.InventoryType);
            resmap.Add("name", folder.Name);
            using (HttpResponse res = httpreq.BeginResponse())
            {
                using (Stream stream = res.GetOutputStream())
                {
                    LLSD_XML.Serialize(resmap, stream);
                }
            }
        }
    }
}
