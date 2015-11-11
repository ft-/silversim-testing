// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
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
                List<KeyValuePair<int, int>> contentranges = new List<KeyValuePair<int, int>>();

                string[] ranges = httpreq["Range"].Split(' ');

                if(ranges.Length > 1)
                {
                    using (HttpResponse httpres = httpreq.BeginResponse("application/vnd.ll.mesh"))
                    {
                        using (Stream o = httpres.GetOutputStream(asset.Data.LongLength))
                        {
                            o.Write(asset.Data, 0, asset.Data.Length);
                        }
                    }
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
                    using (HttpResponse httpres = httpreq.BeginResponse("application/vnd.ll.mesh"))
                    {
                        using (Stream o = httpres.GetOutputStream(asset.Data.LongLength))
                        {
                            o.Write(asset.Data, 0, asset.Data.Length);
                        }
                    }
                    return;
                }

                try
                {
                    start = int.Parse(v[0]);
                    end = string.IsNullOrEmpty(v[1]) ?
                        asset.Data.Length - 1 :
                        int.Parse(v[1]);

                    /* The following check is regarding some weirdness of some viewers trying to retrieve data past the file size.
                     * Yet, RFC2616 requires a RequestedRangeNotSatisfiable here but those viewers would not accept it.
                     */
                    if (start >= asset.Data.Length)
                    {
                        using(HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "application/vnd.ll.mesh"))
                        {

                        }
                        return;
                    }
                    if (start > end)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if (end > asset.Data.Length - 1)
                    {
                        end = asset.Data.Length - 1;
                    }
                    if (end >= asset.Data.Length)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if (start == 0 && end == asset.Data.Length - 1)
                    {
                        using (HttpResponse httpres = httpreq.BeginResponse("application/vnd.ll.mesh"))
                        {
                            using (Stream o = httpres.GetOutputStream(asset.Data.LongLength))
                            {
                                o.Write(asset.Data, 0, asset.Data.Length);
                            }
                        }
                        return;
                    }
                    contentranges.Add(new KeyValuePair<int, int>(start, end));
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception when parsing requested range (GetTexture)", e);
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    return;
                }

                using (HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "application/vnd.ll.mesh"))
                {
                    httpres.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", start, end, asset.Data.Length);
                    using (Stream o = httpres.GetOutputStream(end - start + 1))
                    {
                        o.Write(asset.Data, start, end - start + 1);
                    }
                }
            }
            else
            {
                using (HttpResponse httpres = httpreq.BeginResponse("application/vnd.ll.mesh"))
                {
                    using (Stream o = httpres.GetOutputStream(asset.Data.LongLength))
                    {
                        o.Write(asset.Data, 0, asset.Data.Length);
                    }
                }
            }
        }
    }
}
