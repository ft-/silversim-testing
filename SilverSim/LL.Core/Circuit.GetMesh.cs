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
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void Cap_GetMesh(HttpRequest httpreq)
        {
            string[] parts = httpreq.RawUrl.Substring(1).Split('/');

            if(httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }

            if (parts.Length < 4)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID meshID;
            if (parts[3].Substring(0, 1) != "?")
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
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
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
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
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
            }

            if (asset.Type != AssetType.Mesh)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (httpreq.ContainsHeader("Range"))
            {
                HttpResponse httpres;
                Stream o;
                List<KeyValuePair<int, int>> contentranges = new List<KeyValuePair<int, int>>();

                string[] ranges = httpreq["Range"].Split(' ');

                if(ranges.Length > 1)
                {
                    httpres = httpreq.BeginResponse("application/vnd.ll.mesh");
                    o = httpres.GetOutputStream(asset.Data.LongLength);
                    o.Write(asset.Data, 0, asset.Data.Length);
                    httpres.Close();
                    return;
                }

                int start;
                int end;

                string[] p = ranges[0].Split('=');
                if (p.Length != 2)
                {
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    return;
                }
                string[] v = p[1].Split('-');
                if (v.Length != 2)
                {
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    return;
                }

                if (p[0] != "bytes")
                {
                    httpres = httpreq.BeginResponse("application/vnd.ll.mesh");
                    o = httpres.GetOutputStream(asset.Data.LongLength);
                    o.Write(asset.Data, 0, asset.Data.Length);
                    httpres.Close();
                    return;
                }

                try
                {
                    start = int.Parse(v[0]);
                    if (string.IsNullOrEmpty(v[1]))
                    {
                        end = asset.Data.Length - 1;
                    }
                    else
                    {
                        end = int.Parse(v[1]);
                    }
                    /* The following check is regarding some weirdness of some viewers trying to retrieve data past the file size.
                     * Yet, RFC2616 requires a RequestedRangeNotSatisfiable here but those viewers would not accept it.
                     */
                    if (start >= asset.Data.Length)
                    {
                        httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "application/vnd.ll.mesh").Close();
                        return;
                    }
                    if (start > end)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if (end >= asset.Data.Length)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if (start == 0 && end == asset.Data.Length - 1)
                    {
                        httpres = httpreq.BeginResponse("application/vnd.ll.mesh");
                        o = httpres.GetOutputStream(asset.Data.LongLength);
                        o.Write(asset.Data, 0, asset.Data.Length);
                        httpres.Close();
                        return;
                    }
                    contentranges.Add(new KeyValuePair<int, int>(start, end));
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    return;
                }

                httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "application/vnd.ll.mesh");
                httpres.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", start, end, asset.Data.Length);
                o = httpres.GetOutputStream(end - start + 1);
                o.Write(asset.Data, start, end - start + 1);
                httpres.Close();
            }
            else
            {
                HttpResponse httpres = httpreq.BeginResponse("application/vnd.ll.mesh");
                Stream o = httpres.GetOutputStream(asset.Data.LongLength);
                o.Write(asset.Data, 0, asset.Data.Length);
                httpres.Close();
            }
        }
    }
}
