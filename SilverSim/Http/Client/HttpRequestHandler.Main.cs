// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
    public static partial class HttpRequestHandler
    {
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

        #region HTTP Utility Functions
        static void ReadHeaderLines(AbstractHttpStream s, IDictionary<string, string> headers)
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
                    lastHeader = splits[0].Trim();
                    headers[lastHeader] = splits[1].Trim();
                }
            }
        }
        #endregion

        #region Main HTTP Client Functionality
        public static Stream DoStreamRequest(
            string method, 
            string url, 
            IDictionary<string, string> getValues, 
            string content_type,
            string post, 
            bool compressed, 
            int timeoutms, 
            IDictionary<string, string> headers = null)
        {
            if (getValues != null)
            {
                url += "?" + BuildQueryString(getValues);
            }

            byte[] buffer = new byte[0];
            int content_length = 0;

            if (post.Length != 0)
            {
                buffer = post.ToUTF8Bytes();

                if (compressed || content_type == "application/x-gzip")
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (GZipStream comp = new GZipStream(ms, CompressionMode.Compress))
                        {
                            comp.Write(buffer, 0, buffer.Length);
                            /* The GZIP stream has a CRC-32 and a EOF marker, so we close it first to have it completed */
                        }
                        buffer = ms.ToArray();
                    }
                }

                /* append request POST data */
                content_length = buffer.Length;
            }

            return DoStreamRequest(method, url, getValues, content_type, content_length, delegate(Stream s)
            {
                s.Write(buffer, 0, buffer.Length);
            }, compressed, timeoutms, headers);
        }

        /*---------------------------------------------------------------------*/
        public static Stream DoStreamRequest(
            string method, 
            string url, 
            IDictionary<string, string> getValues, 
            string content_type, 
            int content_length, 
            Action<Stream> postdelegate, 
            bool compressed, 
            int timeoutms, 
            IDictionary<string, string> headers = null)
        {
            if (getValues != null)
            {
                url += "?" + BuildQueryString(getValues);
            }
            Uri uri = new Uri(url);
            byte[] outdata;

            string reqdata = uri.IsDefaultPort ?
                string.Format("{0} {1} HTTP/1.1\r\nHost: {2}\r\nAccept: */*\r\n", method, uri.PathAndQuery, uri.Host) :
                string.Format("{0} {1} HTTP/1.1\r\nHost: {2}:{3}\r\nAccept: */*\r\n", method, uri.PathAndQuery, uri.Host, uri.Port);

            bool doPost = false;
            bool doChunked = false;
            string encval;
            if(headers != null && headers.TryGetValue("Transfer-Encoding", out encval) && encval == "chunked")
            {
                doPost = true;
                doChunked = true;
                reqdata += string.Format("Content-Type: {0}\r\n", content_type);
                if (compressed && content_type != "application/x-gzip")
                {
                    reqdata += "X-Content-Encoding: gzip\r\n";
                }

                reqdata += "Expect: 100-continue\r\n";
            }
            else if (content_type.Length != 0)
            {
                doPost = true;
                reqdata += string.Format("Content-Type: {0}\r\nContent-Length: {1}\r\n", content_type, content_length);
                if (compressed && content_type != "application/x-gzip")
                {
                    reqdata += "X-Content-Encoding: gzip\r\n";
                }

                reqdata += "Expect: 100-continue\r\n";
            }
            if (!SupportsPipelining)
            {
                reqdata += "Connection: close\r\n";
            }

#if DEBUG
            /* disable gzip encoding for debugging */
#else
            reqdata += "Accept-Encoding: gzip\r\n";
#endif
            reqdata += "\r\n";
            outdata = Encoding.ASCII.GetBytes(reqdata);

            int retrycnt = 1;
        retry:
            AbstractHttpStream s = OpenStream(uri.Scheme, uri.Host, uri.Port);
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
                try
                {
                    resline = s.ReadHeaderLine();
                    splits = resline.Split(new char[] { ' ' }, 3);
                    if (splits.Length < 3)
                    {
                        throw new BadHttpResponseException();
                    }

                    if (!splits[0].StartsWith("HTTP/"))
                    {
                        throw new BadHttpResponseException();
                    }

                    if (splits[1] != "100")
                    {
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

                    while (s.ReadHeaderLine().Length != 0)
                    {
                        /* ReadHeaderLine() is all we have to do */
                    }
                    s.ReadTimeout = timeoutms;
                }
                catch(HttpStream.TimeoutException)
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

                if (doChunked)
                {
                    /* append request POST data */
                    using (HttpWriteChunkedBodyStream reqbody = new HttpWriteChunkedBodyStream(s))
                    {
                        postdelegate(reqbody);
                    }
                }
                else
                {
                    /* append request POST data */
                    using (RequestBodyStream reqbody = new RequestBodyStream(s, content_length))
                    {
                        postdelegate(reqbody);
                    }
                }
                s.Flush();
            }

            s.ReadTimeout = timeoutms;
            resline = s.ReadHeaderLine();
            splits = resline.Split(new char[] { ' ' }, 3);
            if (splits.Length < 3)
            {
                throw new BadHttpResponseException();
            }

            if (!splits[0].StartsWith("HTTP/"))
            {
                throw new BadHttpResponseException();
            }

            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            if (splits[1] != "200")
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

            ReadHeaderLines(s, headers);
            s.ReadTimeout = timeoutms;

            string value;
            bool compressedresult = false;
            if (headers.TryGetValue("Content-Encoding", out value) || headers.TryGetValue("X-Content-Encoding", out value))
            {
                /* Content-Encoding */
                /* x-gzip is deprecated but better feel safe about having that */
                if (value == "gzip" || value == "x-gzip")
                {
                    compressedresult = true;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            bool keepalive = true;
            if(splits[0] == "HTTP/1.0")
            {
                keepalive = false;
            }

            if(headers.TryGetValue("Connection", out value))
            {
                if(value.ToLower() == "keep-alive")
                {
                    keepalive = true;
                }
                else if(value.ToLower() == "close")
                {
                    keepalive = false;
                }
            }

            if (headers.TryGetValue("Content-Length", out value))
            {
                long contentLength;
                if(!long.TryParse(value, out contentLength))
                {
                    throw new BadHttpResponseException();
                }
                Stream bs = new ResponseBodyStream(s, contentLength, keepalive, uri.Scheme, uri.Host, uri.Port);
                if(headers.TryGetValue("Transfer-Encoding", out value) && value == "chunked")
                {
                    bs = new HttpReadChunkedBodyStream(bs);
                }
                if (compressedresult)
                {
                    bs = new GZipStream(bs, CompressionMode.Decompress);
                }
                return bs;
            }
            else
            {
                Stream bs = s;
                if (headers.TryGetValue("Transfer-Encoding", out value) && value == "chunked")
                {
                    bs = new HttpReadChunkedBodyStream(bs);
                }
                if (compressedresult)
                {
                    bs = new GZipStream(bs, CompressionMode.Decompress);
                }
                return bs;
            }
        }
        #endregion
    }
}
