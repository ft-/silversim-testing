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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Web;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        private static string BuildQueryString(IDictionary<string, string> parameters)
        {
            var outStr = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                if (outStr.Length != 0)
                {
                    outStr.Append("&");
                }

                string[] names = kvp.Key.Split('?');
                outStr.Append(HttpUtility.UrlEncode(names[0]));
                outStr.Append("=");
                outStr.Append(HttpUtility.UrlEncode(kvp.Value));
            }

            return outStr.ToString();
        }

        /*---------------------------------------------------------------------*/
        [Serializable]
        public class BadHttpResponseException : Exception
        {
            public BadHttpResponseException()
            {
            }

            public BadHttpResponseException(string message)
                : base(message)
            {
            }

            protected BadHttpResponseException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public BadHttpResponseException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        public sealed class Request
        {
            public string Method = "GET";
            public string Url = string.Empty;
            public IDictionary<string, string> GetValues;
            public string RequestContentType;
            public int RequestContentLength;
            public string RequestBody;
            public Action<Stream> RequestBodyDelegate;
            public bool IsCompressed;
            public int TimeoutMs = 20000;
            public ConnectionReuseMode ReuseMode = ConnectionReuse;
            public IDictionary<string, string> Headers;
            public bool Expect100Continue;
            public bool UseChunkedEncoding;
        }

        #region HTTP Utility Functions
        private static void ReadHeaderLines(AbstractHttpStream s, IDictionary<string, string> headers)
        {
            headers.Clear();
            string lastHeader = string.Empty;
            string hdrline;
            while ((hdrline = s.ReadHeaderLine()).Length != 0)
            {
                if (hdrline.StartsWith(" "))
                {
                    if (lastHeader.Length != 0)
                    {
                        headers[lastHeader] += hdrline.Trim();
                    }
                }
                else
                {
                    string[] splits = hdrline.Split(new char[] { ':' }, 2);
                    if (splits.Length < 2)
                    {
                        throw new BadHttpResponseException();
                    }
                    lastHeader = splits[0].Trim().ToLowerInvariant();
                    headers[lastHeader] = splits[1].Trim();
                }
            }
        }
        #endregion

        #region Main HTTP Client Functionality
        public static string ExecuteRequest(
            this Request request)
        {
            using (Stream responseStream = ExecuteStreamRequest(request))
            using (var reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
            }
        }

        public static Stream ExecuteStreamRequest(
            this Request request)
        {
            string postdata = request.RequestBody;
            if (postdata != null)
            {
                var buffer = new byte[0];
                request.RequestContentLength = 0;

                buffer = postdata.ToUTF8Bytes();

                if (request.IsCompressed || request.RequestContentType == "application/x-gzip")
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var comp = new GZipStream(ms, CompressionMode.Compress))
                        {
                            comp.Write(buffer, 0, buffer.Length);
                            /* The GZIP stream has a CRC-32 and a EOF marker, so we close it first to have it completed */
                        }
                        buffer = ms.ToArray();
                    }
                }

                request.RequestBodyDelegate = (Stream poststream) => poststream.Write(buffer, 0, buffer.Length);

                /* append request POST data */
                request.RequestContentLength = buffer.Length;
            }

            string url = request.Url;
            if (request.GetValues != null)
            {
                url += "?" + BuildQueryString(request.GetValues);
            }

            var uri = new Uri(url);

            if (request.ReuseMode == ConnectionReuseMode.Http2PriorKnowledge)
            {
                return DoStreamRequestHttp2(request, uri);
            }

            if(request.ReuseMode == ConnectionReuseMode.UpgradeHttp2)
            {
                Http2Connection.Http2Stream h2stream = TryReuseStream(uri.Scheme, uri.Host, uri.Port);
                if(h2stream != null)
                {
                    return DoStreamRequestHttp2(request, uri, h2stream);
                }
            }

            byte[] outdata;

            string method = request.Method;

            string reqdata = uri.IsDefaultPort ?
                    $"{method} {uri.PathAndQuery} HTTP/1.1\r\nHost: {uri.Host}\r\nAccept: */*\r\n":
                    $"{method} {uri.PathAndQuery} HTTP/1.1\r\nHost: {uri.Host}:{uri.Port}\r\nAccept: */*\r\n";

            bool doPost = false;
            bool doChunked = false;
            bool compressed = request.IsCompressed;
            int content_length = request.RequestContentLength;

            IDictionary<string, string> headers = request.Headers;
            if(headers != null)
            {
                var removal = new List<string>();
                foreach(string k in headers.Keys)
                {
                    if(string.Compare(k, "content-length", true) == 0 ||
                        string.Compare(k, "content-type", true) == 0 ||
                        string.Compare(k, "connection", true) == 0 ||
                        string.Compare(k, "expect", true) == 0 ||
                        string.Compare(k, "transfer-encoding", true) == 0)
                    {
                        removal.Add(k);
                    }
                }
                if (removal.Count != 0)
                {
                    foreach (string k in removal)
                    {
                        headers.Remove(k);
                    }
                }
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    reqdata += $"{kvp.Key}: {kvp.Value}\r\n";
                }
                if(request.UseChunkedEncoding)
                {
                    reqdata += "Transfer-Encoding: chunked\r\n";
                }
            }

            string content_type = request.RequestContentType;
            bool expect100Continue = request.Expect100Continue;

            if (request.UseChunkedEncoding)
            {
                doPost = true;
                doChunked = true;
                reqdata += $"Content-Type: {content_type}\r\n";
                if (compressed && content_type != "application/x-gzip")
                {
                    reqdata += "X-Content-Encoding: gzip\r\n";
                }

                if (expect100Continue)
                {
                    reqdata += "Expect: 100-continue\r\n";
                }
            }
            else if (content_type.Length != 0)
            {
                doPost = true;
                reqdata += $"Content-Type: {content_type}\r\nContent-Length: {content_length}\r\n";
                if (compressed && content_type != "application/x-gzip")
                {
                    reqdata += "X-Content-Encoding: gzip\r\n";
                }

                if (expect100Continue)
                {
                    reqdata += "Expect: 100-continue\r\n";
                }
            }

            if(method != "HEAD")
            {
                reqdata += "Accept-Encoding: gzip, deflate\r\n";
            }

            int retrycnt = 1;
            retry:
            AbstractHttpStream s = OpenStream(uri.Scheme, uri.Host, uri.Port, request.ReuseMode);
            string finalreqdata = reqdata;
            if (!s.IsReusable)
            {
                finalreqdata += "Connection: close\r\n";
            }
            bool h2cUpgrade = request.ReuseMode == ConnectionReuseMode.UpgradeHttp2;
            if(h2cUpgrade)
            {
                finalreqdata += "Upgrade: h2c\r\nHTTP2-Settings:\r\n";
            }
            finalreqdata += "\r\n";
            outdata = Encoding.ASCII.GetBytes(finalreqdata);

            try
            {
                s.Write(outdata, 0, outdata.Length);
                s.Flush();
            }
            catch(ObjectDisposedException)
            {
                if (retrycnt-- > 0)
                {
                    goto retry;
                }
                throw;
            }
            catch(SocketException)
            {
                if (retrycnt-- > 0)
                {
                    goto retry;
                }
                throw;
            }
            catch (IOException)
            {
                if(retrycnt-- > 0)
                {
                    goto retry;
                }
                throw;
            }
            s.ReadTimeout = 10000;
            string resline;
            string[] splits;

            if (doPost)
            {
                if (expect100Continue)
                {
                    try
                    {
                        resline = s.ReadHeaderLine();
                        splits = resline.Split(new char[] { ' ' }, 3);
                        if (splits.Length < 3)
                        {
                            throw new BadHttpResponseException("Not a HTTP response");
                        }

                        if (!splits[0].StartsWith("HTTP/"))
                        {
                            throw new BadHttpResponseException("Missing HTTP version info");
                        }

                        if (splits[1] == "101")
                        {
                            ReadHeaderLines(s, headers);
                            headers.Clear();
                            var conn = new Http2Connection(s, false);
                            Http2Connection.Http2Stream h2stream = conn.UpgradeClientStream();
                            AddH2Connection(conn, uri.Scheme, uri.Host, uri.Port);

                            return DoStreamRequestHttp2Response(h2stream, request, uri, doPost);
                        }

                        if (splits[1] != "100")
                        {
                            int statusCode;
                            if (!int.TryParse(splits[1], out statusCode))
                            {
                                statusCode = 500;
                            }
                            if (statusCode == 404)
                            {
                                throw new HttpException(statusCode, splits[2] + " (" + url + ")");
                            }
                            else
                            {
                                throw new HttpException(statusCode, splits[2]);
                            }
                        }

                        while (s.ReadHeaderLine().Length != 0)
                        {
                            /* ReadHeaderLine() is all we have to do */
                        }
                    }
                    catch (HttpStream.TimeoutException)
                    {
                        /* keep caller from being exceptioned */
                    }
                    catch (IOException)
                    {
                        /* keep caller from being exceptioned */
                    }
                    catch (SocketException)
                    {
                        /* keep caller from being exceptioned */
                    }
                }

                if (doChunked)
                {
                    /* append request POST data */
                    using (var reqbody = new HttpWriteChunkedBodyStream(s))
                    {
                        request.RequestBodyDelegate(reqbody);
                    }
                }
                else
                {
                    /* append request POST data */
                    using (var reqbody = new RequestBodyStream(s, content_length))
                    {
                        request.RequestBodyDelegate(reqbody);
                    }
                }
                s.Flush();
            }

            s.ReadTimeout = request.TimeoutMs;
            resline = s.ReadHeaderLine();
            splits = resline.Split(new char[] { ' ' }, 3);
            if (splits.Length < 3)
            {
                throw new BadHttpResponseException("Not a HTTP response");
            }

            if (!splits[0].StartsWith("HTTP/"))
            {
                throw new BadHttpResponseException("Missing HTTP version info");
            }

            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }
            else
            {
                headers.Clear();
            }

            if(splits[1] == "101")
            {
                ReadHeaderLines(s, headers);
                headers.Clear();
                var conn = new Http2Connection(s, false);
                Http2Connection.Http2Stream h2stream = conn.UpgradeClientStream();
                AddH2Connection(conn, uri.Scheme, uri.Host, uri.Port);

                return DoStreamRequestHttp2Response(h2stream, request, uri, doPost);
            }

            if (!splits[1].StartsWith("2"))
            {
                ReadHeaderLines(s, headers);
                int statusCode;
                if(!int.TryParse(splits[1], out statusCode))
                {
                    statusCode = 500;
                }
                if (statusCode == 404)
                {
                    throw new HttpException(statusCode, splits[2] + " (" + url + ")");
                }
                else
                {
                    throw new HttpException(statusCode, splits[2]);
                }
            }
            
            if(headers != null)
            {
                /* needs a little passthrough for not changing the API, this actually comes from HTTP/2 */
                headers.Add(":status", splits[1]);
            }

            ReadHeaderLines(s, headers);

            string value;
            string compressedresult = string.Empty;
            if (headers.TryGetValue("content-encoding", out value) || headers.TryGetValue("x-content-encoding", out value))
            {
                /* Content-Encoding */
                /* x-gzip is deprecated but better feel safe about having that */
                if (value == "gzip" || value == "x-gzip")
                {
                    compressedresult = "gzip";
                }
                else if(value == "deflate")
                {
                    compressedresult = "deflate";
                }
                else
                {
                    throw new NotSupportedException("Unsupport content-encoding");
                }
            }

            if(splits[0] == "HTTP/1.0")
            {
                s.IsReusable = false;
            }

            if(headers.TryGetValue("connection", out value))
            {
                value = value.Trim().ToLower();
                if(value == "keep-alive")
                {
                    s.IsReusable = true;
                }
                else if(value == "close")
                {
                    s.IsReusable = false;
                }
            }

            if (method == "HEAD")
            {
                /* HEAD does not have any response data */
                return new ResponseBodyStream(s, 0, uri.Scheme, uri.Host, uri.Port);
            }
            else if (headers.TryGetValue("content-length", out value))
            {
                long contentLength;
                if(!long.TryParse(value, out contentLength))
                {
                    throw new BadHttpResponseException("Unparseable length in response");
                }
                Stream bs = new ResponseBodyStream(s, contentLength, uri.Scheme, uri.Host, uri.Port);
                if(headers.TryGetValue("transfer-encoding", out value) && value == "chunked")
                {
                    bs = new HttpReadChunkedBodyStream(bs);
                }
                if(compressedresult.Length != 0)
                {
                    if (compressedresult == "gzip")
                    {
                        bs = new GZipStream(bs, CompressionMode.Decompress);
                    }
                    else if (compressedresult == "deflate")
                    {
                        bs = new DeflateStream(bs, CompressionMode.Decompress);
                    }
                }
                return bs;
            }
            else
            {
                Stream bs = s;
                if (headers.TryGetValue("transfer-encoding", out value) && value == "chunked")
                {
                    bs = new BodyResponseChunkedStream(s, uri.Scheme, uri.Host, uri.Port);
                }
                else
                {
                    s.IsReusable = false;
                }
                if (compressedresult.Length != 0)
                {
                    if (compressedresult == "gzip")
                    {
                        bs = new GZipStream(bs, CompressionMode.Decompress);
                    }
                    else if (compressedresult == "deflate")
                    {
                        bs = new DeflateStream(bs, CompressionMode.Decompress);
                    }
                }
                return bs;
            }
        }

        private static Stream DoStreamRequestHttp2(
            Request request,
            Uri uri,
            Http2Connection.Http2Stream reuseStream = null)
        {
            bool doPost = false;
            string encval;

            var actheaders = new Dictionary<string, string>();
            actheaders.Add(":method", request.Method);
            actheaders.Add(":scheme", uri.Scheme);
            actheaders.Add(":path", uri.PathAndQuery);
            if (uri.IsDefaultPort)
            {
                actheaders.Add(":authority", uri.Host);
            }
            else
            {
                actheaders.Add(":authority", $"{uri.Host}:{uri.Port}");
            }

            IDictionary<string, string> headers = request.Headers;
            string content_type = request.RequestContentType;
            int content_length = request.RequestContentLength;
            bool compressed = request.IsCompressed;
            bool expect100Continue = request.Expect100Continue;

            if (headers != null)
            {
                var removal = new List<string>();
                foreach (KeyValuePair<string, string> k in headers)
                {
                    string kn = k.Key.ToLower();
                    if (kn == "content-length" ||
                        kn == "content-type" ||
                        kn == "connection" ||
                        kn == "expect")
                    {
                        continue;
                    }
                    actheaders.Add(kn, k.Value);
                }
            }

            if (actheaders.TryGetValue("transfer-encoding", out encval) && encval == "chunked")
            {
                actheaders.Remove("transfer-encoding");
            }

            if (content_type.Length != 0)
            {
                doPost = true;
                actheaders.Add("content-type", content_type);
                actheaders.Add("content-length", content_length.ToString());
                if (compressed && content_type != "application/x-gzip")
                {
                    actheaders.Add("x-content-encoding", "gzip");
                }

                if (expect100Continue)
                {
                    actheaders.Add("expect", "100-continue");
                }
            }

            if (request.Method != "HEAD")
            {
                actheaders.Add("Accept-Encoding", "gzip, deflate");
            }

            int retrycnt = 1;
            retry:
            Http2Connection.Http2Stream s = reuseStream ?? OpenHttp2Stream(uri.Scheme, uri.Host, uri.Port);
            headers.Clear();
            try
            {
                s.SendHeaders(actheaders, 0, !doPost);
            }
            catch (ObjectDisposedException)
            {
                if (retrycnt-- > 0)
                {
                    goto retry;
                }
                throw;
            }
            catch (SocketException)
            {
                if (retrycnt-- > 0)
                {
                    goto retry;
                }
                throw;
            }
            catch (IOException)
            {
                if (retrycnt-- > 0)
                {
                    goto retry;
                }
                throw;
            }
            s.ReadTimeout = 10000;

            return DoStreamRequestHttp2Response(
                s,
                request,
                uri,
                doPost);
        }

        private static Stream DoStreamRequestHttp2Response(
            Http2Connection.Http2Stream s,
            Request request,
            Uri uri,
            bool doPost)
        {
            Dictionary<string, string> rxheaders;
            if (doPost)
            {
                if (request.Expect100Continue)
                {
                    rxheaders = s.ReceiveHeaders();
                    string status;
                    if (!rxheaders.TryGetValue(":status", out status))
                    {
                        s.SendRstStream(Http2Connection.Http2ErrorCode.ProtocolError);
                        throw new BadHttpResponseException("Not a HTTP response");
                    }

                    if (status != "100")
                    {
                        int statusCode;
                        if (!int.TryParse(status, out statusCode))
                        {
                            statusCode = 500;
                        }
                        if (statusCode == 404)
                        {
                            s.SendRstStream(Http2Connection.Http2ErrorCode.StreamClosed);
                            throw new HttpException(statusCode, statusCode.ToString() + " (" + uri + ")");
                        }
                        else
                        {
                            s.SendRstStream(Http2Connection.Http2ErrorCode.StreamClosed);
                            throw new HttpException(statusCode, statusCode.ToString());
                        }
                    }
                }

                /* append request POST data */
                request.RequestBodyDelegate(s);
                s.SendEndOfStream();
            }

            s.ReadTimeout = request.TimeoutMs;
            rxheaders = s.ReceiveHeaders();

            IDictionary<string, string> headers = request.Headers;
            if(headers != null)
            {
                headers.Clear();
                foreach (KeyValuePair<string, string> kvp in rxheaders)
                {
                    headers.Add(kvp.Key, kvp.Value);
                }
            }
            string statusVal;
            if(!rxheaders.TryGetValue(":status", out statusVal))
            {
                s.SendRstStream(Http2Connection.Http2ErrorCode.ProtocolError);
                throw new BadHttpResponseException("Missing status");
            }

            if (!statusVal.StartsWith("2"))
            {
                int statusCode;
                if (!int.TryParse(statusVal, out statusCode))
                {
                    statusCode = 500;
                }
                if (statusCode == 404)
                {
                    s.SendRstStream(Http2Connection.Http2ErrorCode.StreamClosed);
                    throw new HttpException(statusCode, statusVal + " (" + uri.ToString() + ")");
                }
                else
                {
                    s.SendRstStream(Http2Connection.Http2ErrorCode.StreamClosed);
                    throw new HttpException(statusCode, statusVal);
                }
            }

            string value;
            string compressedresult = string.Empty;
            if (headers.TryGetValue("content-encoding", out value) || headers.TryGetValue("x-content-encoding", out value))
            {
                /* Content-Encoding */
                /* x-gzip is deprecated but better feel safe about having that */
                if (value == "gzip" || value == "x-gzip")
                {
                    compressedresult = "gzip";
                }
                else if(value == "deflate")
                {
                    compressedresult = "deflate";
                }
                else
                {
                    throw new NotSupportedException("Unsupport content-encoding");
                }
            }

            if (request.Method == "HEAD")
            {
                /* HEAD does not have any response data */
                return s;
            }
            else
            {
                Stream bs = s;
                if (compressedresult.Length != 0)
                {
                    if (compressedresult == "gzip")
                    {
                        bs = new GZipStream(bs, CompressionMode.Decompress);
                    }
                    else if (compressedresult == "deflate")
                    {
                        bs = new DeflateStream(bs, CompressionMode.Decompress);
                    }
                }
                return bs;
            }
        }
        #endregion
    }
}
