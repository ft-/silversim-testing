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
        public void Cap_CreateInventoryCategory(HttpRequest httpreq)
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
            InventoryFolder folder = new InventoryFolder();
            folder.ID = reqmap["folder_id"].AsUUID;
            folder.ParentFolderID = reqmap["parent_id"].AsUUID;
            folder.InventoryType = (InventoryType)reqmap["type"].AsInt;
            folder.Name = reqmap["name"].ToString();
            folder.Version = 1;

            try
            {
                Agent.InventoryService.Folder.Add(AgentID, folder);
            }
            catch
            {
                httpreq.BeginResponse(HttpStatusCode.InternalServerError, "Internal Server Error").Close();
                return;
            }

            Map resmap = new Map();
            resmap.Add("folder_id", folder.ID);
            resmap.Add("parent_id", folder.ParentFolderID);
            resmap.Add("type", (int)folder.InventoryType);
            resmap.Add("name", folder.Name);
            HttpResponse res = httpreq.BeginResponse();
            LLSD_XML.Serialize(resmap, res.GetOutputStream());
            res.Close();
        }
    }
}
