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

using log4net;
using System;
using System.Collections.Generic;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    public static class RangeExtensionMethods
    {
        private static readonly ILog m_Log = LogManager.GetLogger("HTTP RANGE");
        public static void RangeResponse(this HttpRequest httpreq, byte[] data, string contentType, string debug_info)
        {
            if (httpreq.ContainsHeader("Range"))
            {
                var contentranges = new List<KeyValuePair<int, int>>();

                var ranges = httpreq["Range"].Split(' ');

                if (ranges.Length > 1)
                {
                    using (var httpres = httpreq.BeginResponse(contentType))
                    {
                        using (var o = httpres.GetOutputStream(data.LongLength))
                        {
                            o.Write(data, 0, data.Length);
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
                    using (var httpres = httpreq.BeginResponse(contentType))
                    {
                        using (var o = httpres.GetOutputStream(data.LongLength))
                        {
                            o.Write(data, 0, data.Length);
                        }
                    }
                    return;
                }

                try
                {
                    start = int.Parse(v[0]);
                    end = string.IsNullOrEmpty(v[1]) ?
                        data.Length - 1 :
                        int.Parse(v[1]);

                    /* The following check is regarding some weirdness of some viewers trying to retrieve data past the file size.
                     * Yet, RFC2616 requires a RequestedRangeNotSatisfiable here but those viewers would not accept it.
                     */
                    if (start >= data.Length)
                    {
                        using (var httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "application/vnd.ll.mesh"))
                        {
                            /* no additional action needed here */
                        }
                        return;
                    }
                    if (start > end)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if (end > data.Length - 1)
                    {
                        end = data.Length - 1;
                    }
                    if (end >= data.Length)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if (start == 0 && end == data.Length - 1)
                    {
                        using (var httpres = httpreq.BeginResponse(contentType))
                        {
                            using (var o = httpres.GetOutputStream(data.LongLength))
                            {
                                o.Write(data, 0, data.Length);
                            }
                        }
                        return;
                    }
                    contentranges.Add(new KeyValuePair<int, int>(start, end));
                }
                catch (Exception e)
                {
                    m_Log.Debug("Exception when parsing requested range (" + debug_info + ")", e);
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    return;
                }

                using (var httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "application/vnd.ll.mesh"))
                {
                    httpres.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", start, end, data.Length);
                    using (var o = httpres.GetOutputStream(end - start + 1))
                    {
                        o.Write(data, start, end - start + 1);
                    }
                }
            }
            else
            {
                using (var httpres = httpreq.BeginResponse(contentType))
                {
                    using (var o = httpres.GetOutputStream(data.LongLength))
                    {
                        o.Write(data, 0, data.Length);
                    }
                }
            }
        }
    }
}
