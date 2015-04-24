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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;

namespace SilverSim.Main.Common.HttpClient
{
    public static partial class HttpRequestHandler
    {
        /*---------------------------------------------------------------------*/
        public class BadHttpResponseException : Exception
        {
            public BadHttpResponseException()
            {

            }
        }

        #region HTTP Utility Functions
        private static string ReadHeaderLine(Stream s)
        {
            int c;
            string headerLine = string.Empty;
            while ((c = s.ReadByte()) != '\r')
            {
                if (c == -1)
                {
                    throw new BadHttpResponseException();
                }
                headerLine += (char)c;
            }

            if (s.ReadByte() != '\n')
            {
                throw new BadHttpResponseException();
            }

            return headerLine;
        }

        static void ReadHeaderLines(Stream s, IDictionary<string, string> headers)
        {
            headers.Clear();
            string lastHeader = "";
            string hdrline;
            while ((hdrline = ReadHeaderLine(s)) != "")
            {
                if (hdrline.StartsWith(" "))
                {
                    if (lastHeader != string.Empty)
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

            if (post != string.Empty)
            {
                buffer = System.Text.Encoding.UTF8.GetBytes(post);

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
        public delegate void StreamPostDelegate(Stream output);
        public static Stream DoStreamRequest(
            string method, 
            string url, 
            IDictionary<string, string> getValues, 
            string content_type, 
            int content_length, 
            StreamPostDelegate postdelegate, 
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
            string reqdata;
            if (uri.IsDefaultPort)
            {
                reqdata = string.Format("{0} {1} HTTP/1.1\r\nHost: {2}\r\nAccept: */*\r\n", method, uri.PathAndQuery, uri.Host);
            }
            else
            {
                reqdata = string.Format("{0} {1} HTTP/1.1\r\nHost: {2}:{3}\r\nAccept: */*\r\n", method, uri.PathAndQuery, uri.Host, uri.Port);
            }
            bool doPost = false;
            if (content_type != string.Empty)
            {
                doPost = true;
                reqdata += string.Format("Content-Type: {0}\r\nContent-Length: {1}\r\n", content_type, content_length);
                if (compressed && content_type != "application/x-gzip")
                {
                    reqdata += "X-Content-Encoding: gzip\r\n";
                }


                reqdata += "Expect: 100-continue\r\n";
            }

            reqdata += "Accept-Encoding: gzip\r\n\r\n";
            outdata = Encoding.ASCII.GetBytes(reqdata);

            int retrycnt = 1;
        retry:
            Stream s = OpenStream(uri.Scheme, uri.Host, uri.Port);
            s.WriteTimeout = timeoutms;
            try
            {
                s.Write(outdata, 0, outdata.Length);
            }
            catch(IOException)
            {
                if(retrycnt-- > 0)
                {
                    goto retry;
                }
            }
            s.ReadTimeout = 10000;
            string resline;
            string[] splits;

            if (doPost)
            {
                try
                {
                    resline = ReadHeaderLine(s);
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
                        throw new HttpException(int.Parse(splits[1]), splits[2]);
                    }

                    while (ReadHeaderLine(s) != "")
                    {

                    }
                }
                catch (IOException)
                {

                }

                /* append request POST data */
                using (RequestBodyStream reqbody = new RequestBodyStream(s, content_length))
                {
                    postdelegate(reqbody);
                }
            }

            s.ReadTimeout = timeoutms;
            resline = ReadHeaderLine(s);
            splits = resline.Split(new char[] { ' ' }, 3);
            if (splits.Length < 3)
            {
                throw new BadHttpResponseException();
            }

            if (!splits[0].StartsWith("HTTP/"))
            {
                throw new BadHttpResponseException();
            }

            if (splits[1] != "200")
            {
                throw new HttpException(int.Parse(splits[1]), splits[2]);
            }

            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }
            ReadHeaderLines(s, headers);

            string value;
            bool compressedresult = false;
            if (headers.TryGetValue("Content-Encoding", out value) || headers.TryGetValue("X-Content-Encoding", out value))
            {
                /* Content-Encoding */
                if (value == "gzip")
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
                Stream bs = new ResponseBodyStream(s, long.Parse(value), keepalive, uri.Scheme, uri.Host, uri.Port);
                if (compressedresult)
                {
                    return new GZipStream(bs, CompressionMode.Decompress);
                }
                else
                {
                    return bs;
                }
            }
            else
            {
                return s;
            }
        }
        #endregion
    }
}
