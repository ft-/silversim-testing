﻿/*

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
using System.IO;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types.Inventory;
using SilverSim.Types.Asset;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void Cap_GetMesh(HttpRequest httpreq)
        {
            string[] parts = httpreq.RawUrl.Substring(1).Split('/');

            if(httpreq.Method != "GET")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed").Close();
                return;
            }

            if (parts.Length < 4)
            {
                httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
                return;
            }

            UUID meshID;
            if (parts[3].Substring(0, 1) != "?")
            {
                httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
                return;
            }

            string meshreq = parts[3].Substring(1);
            string[] texreq = meshreq.Split('&');
            string mID = string.Empty;

            foreach (string texreqentry in texreq)
            {
                if (texreqentry.StartsWith("mesh_id="))
                {
                    mID = texreqentry.Substring(8);
                }
            }

            try
            {
                meshID = UUID.Parse(mID);
            }
            catch
            {
                httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
                return;
            }

            AssetData asset;
            try
            {
                /* let us prefer the sim asset service */
                asset = Scene.AssetService[meshID];
            }
            catch
            {
                try
                {
                    asset = Agent.AssetService[meshID];
                    try
                    {
                        /* try to store the asset on our sim's asset service */
                        Scene.AssetService.Store(asset);
                    }
                    catch
                    {

                    }
                }
                catch
                {
                    httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
                    return;
                }
            }

            if (asset.Type != AssetType.Mesh)
            {
                httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found").Close();
                return;
            }

            HttpResponse httpres = httpreq.BeginResponse();
            Stream o = httpres.GetOutputStream(asset.Data.LongLength);
            o.Write(asset.Data, 0, asset.Data.Length);
            httpres.Close();
        }
    }
}
