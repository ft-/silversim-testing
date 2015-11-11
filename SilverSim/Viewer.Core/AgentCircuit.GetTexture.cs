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
        void Cap_GetTexture(HttpRequest httpreq)
        {
            string[] parts = httpreq.RawUrl.Substring(1).Split('/');

            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }

            if (parts.Length < 4)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID textureID;
            if(parts[3].Substring(0, 1) != "?")
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            string texturereq = parts[3].Substring(1);
            string[] texreq = texturereq.Split('&');
            string texID = string.Empty;

            foreach(string texreqentry in texreq)
            {
                if(texreqentry.StartsWith("texture_id="))
                {
                    texID = texreqentry.Substring(11);
                }
            }
            try
            {
                textureID = UUID.Parse(texID);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            bool j2k_accepted = true;
            if(httpreq.ContainsHeader("Accept"))
            {
                j2k_accepted = false;
                parts = httpreq["Accept"].Split(',');
                foreach(string p in parts)
                {
                    string[] comps = p.Trim().Split(';');
                    if (comps[0].Trim() == "image/x-j2c")
                    {
                        j2k_accepted = true;
                    }
                }
                if (!j2k_accepted)
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotAcceptable, "Not acceptable");
                    return;
                }
            }

            AssetData asset;
            try
            {
                /* let us prefer the sim asset service */
                asset = Scene.AssetService[textureID];
            }
            catch(Exception e1)
            {
                try
                {
                    asset = Agent.AssetService[textureID];
                    try
                    {
                        /* try to store the asset on our sim's asset service */
                        asset.Temporary = true;
                        Scene.AssetService.Store(asset);
                    }
                    catch(Exception e3)
                    {
                        m_Log.DebugFormat("Failed to store asset {0} locally (Cap_GetTexture): {1}", textureID, e3.Message);
                    }
                }
                catch(Exception e2)
                {
                    if (Server.LogAssetFailures)
                    {
                        m_Log.DebugFormat("Failed to download image {0} (Cap_GetTexture): {1} or {2}\nA: {3}\nB: {4}", textureID, e1.Message, e2.Message, e1.StackTrace, e2.StackTrace);
                    }
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
            }

            if(asset.Type != AssetType.Texture)
            {
                if(asset.Type != AssetType.Mesh)
                {
                    m_Log.DebugFormat("Failed to download image (Cap_GetTexture): Viewer for AgentID {0} tried to download non-texture asset ({1})", AgentID, asset.Type.ToString());
                }
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (httpreq.ContainsHeader("Range"))
            {
                string[] ranges = httpreq["Range"].Split(' ');
                if(ranges.Length > 1)
                {
                    using (HttpResponse httpres = httpreq.BeginResponse("image/x-j2c"))
                    {
                        using (Stream o = httpres.GetOutputStream(asset.Data.LongLength))
                        {
                            o.Write(asset.Data, 0, asset.Data.Length);
                        }
                    }
                    return;
                }

                string[] p = ranges[0].Split('=');
                if(p.Length != 2)
                {
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    m_Log.WarnFormat("Requested range for GetTexture is not decoded. {0}", ranges[0]);
                    return;
                }
                string[] v = p[1].Split('-');
                if(v.Length != 2)
                {
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    m_Log.WarnFormat("Requested range for GetTexture is not decoded. {0} see {1}", ranges[0], p[1]);
                    return;
                }

                if(p[0] != "bytes")
                {
                    using (HttpResponse httpres = httpreq.BeginResponse("image/x-j2c"))
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

                try
                {
                    start = int.Parse(v[0]);
                    end = string.IsNullOrEmpty(v[1]) ?
                        asset.Data.Length - 1 :
                        int.Parse(v[1]);

                    /* The following check is regarding some weirdness of some viewers trying to retrieve data past the file size.
                     * Yet, RFC2616 requires a RequestedRangeNotSatisfiable here but those viewers would not accept it.
                     */
                    if(start >= asset.Data.Length)
                    {
                        using(HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "image/x-j2c"))
                        {

                        }
                        return;
                    }
                    if (start > end)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if(end > asset.Data.Length - 1)
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
                        using (HttpResponse httpres = httpreq.BeginResponse("image/x-j2c"))
                        {
                            using (Stream o = httpres.GetOutputStream(asset.Data.LongLength))
                            {
                                o.Write(asset.Data, 0, asset.Data.Length);
                            }
                        }
                        return;
                    }
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception when parsing requested range (GetTexture)", e);
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    return;
                }

                using (HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "image/x-j2c"))
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
                using (HttpResponse httpres = httpreq.BeginResponse("image/x-j2c"))
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
