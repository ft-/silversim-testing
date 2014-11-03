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
using System.Net;
using System.IO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types.Asset;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void Cap_UploadBakedTexture(HttpRequest httpreq)
        {
            if (httpreq.Method != "POST")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            string[] parts = httpreq.RawUrl.Substring(1).Split('/');
            if(parts.Length == 3)
            {
                /* Upload start */
                Map llsdreply = new Map();
                llsdreply.Add("state", new AString("upload"));
                llsdreply.Add("uploader", httpreq.RawUrl + "/Upload");

                HttpResponse httpres = httpreq.BeginResponse();
                Stream outStream = httpres.GetOutputStream();
                LLSD_XML.Serialize(llsdreply, outStream);
                httpres.Close();
            }
            else if(parts[3] != "Upload")
            {
                httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
            }
            else
            {
                /* Upload finished */
                AssetData asset = new AssetData();
                Stream body = httpreq.Body;
                asset.Data = new byte[body.Length];
                if(body.Length != body.Read(asset.Data, 0, (int)body.Length))
                {
                    httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
                    return;
                }

                asset.Type = AssetType.Texture;
                asset.ID = UUID.RandomFixedFirst(0xFFFFFFFF);
                asset.Local = true;
                asset.Temporary = true;
                asset.Name = "Baked Texture for Agent " + AgentID;
                asset.Creator = Agent.Owner;

                try
                {
                    Scene.AssetService.Store(asset);
                }
                catch
                {
                    httpreq.BeginResponse(HttpStatusCode.InternalServerError, "Internal Server Error").Close();
                    return;
                }
                
                Map llsdreply = new Map();
                llsdreply.Add("new_asset", asset.ID);
                llsdreply.Add("new_inventory_item", UUID.Zero);
                llsdreply.Add("state", "complete");

                HttpResponse httpres = httpreq.BeginResponse();
                Stream outStream = httpres.GetOutputStream();
                LLSD_XML.Serialize(llsdreply, outStream);
                httpres.Close();
            }
        }
    }
}
