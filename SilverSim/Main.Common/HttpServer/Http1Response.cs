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

using SilverSim.Http;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class Http1Response : HttpResponse
    {
        private readonly Stream m_Output;
        private bool m_IsHeaderSent;
        private Stream ResponseBody;
        private readonly bool IsChunkedAccepted;

        public Http1Response(Stream output, HttpRequest request, HttpStatusCode statusCode, string statusDescription)
            : base(request, statusCode, statusDescription)
        {
            Headers["Content-Type"] = "text/html";
            m_Output = output;
            MajorVersion = request.MajorVersion;
            MinorVersion = request.MinorVersion;
            IsCloseConnection = HttpConnectionMode.Close == request.ConnectionMode;
            if(IsCloseConnection)
            {
                Headers["Connection"] = "close";
            }
            else
            {
                Headers["Connection"] = "keep-alive";
            }
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            IsChunkedAccepted = request.ContainsHeader("TE");
            if (MinorVersion >= 1 || MajorVersion > 1)
            {
                IsChunkedAccepted = true;
            }
        }

        protected override void SendHeaders()
        {
            using (var ms = new MemoryStream())
            {
                using (var w = ms.UTF8StreamWriter())
                {
                    w.Write(string.Format("HTTP/{0}.{1} {2} {3}\r\n", MajorVersion, MinorVersion, (uint)StatusCode, StatusDescription.Replace("\n", string.Empty).Replace("\r", string.Empty)));
                    foreach (KeyValuePair<string, string> kvp in Headers)
                    {
                        w.Write(string.Format("{0}: {1}\r\n", kvp.Key.Replace("\r", string.Empty).Replace("\n", string.Empty), kvp.Value.Replace("\r", string.Empty).Replace("\n", string.Empty)));
                    }
                    w.Write("\r\n");
                }
                byte[] b = ms.ToArray();
                m_Output.Write(b, 0, b.Length);
                m_Output.Flush();
            }
            m_IsHeaderSent = true;
        }

        public override void Close()
        {
            if (!m_IsHeaderSent)
            {
                Headers["Content-Length"] = "0";
                SendHeaders();
            }
            if (ResponseBody != null)
            {
                ResponseBody.Close();
                ResponseBody = null;
            }
            m_Output.Flush();

            if (IsCloseConnection)
            {
                m_Output.Close();
                throw new ConnectionCloseException();
            }
        }

        public override Stream GetOutputStream(long contentLength)
        {
            if (!m_IsHeaderSent)
            {
                Headers["Content-Length"] = contentLength.ToString();
                SendHeaders();
            }
            else
            {
                throw new InvalidOperationException();
            }

            return ResponseBody = new HttpResponseBodyStream(m_Output, contentLength);
        }

        public override Stream GetOutputStream(bool disableCompression = false)
        {
            bool gzipEnable = false;
            if (!m_IsHeaderSent)
            {
                if (!IsChunkedAccepted)
                {
                    IsCloseConnection = true;
                }
                Headers.Remove("Content-Length");
                if (!disableCompression && AcceptedEncodings != null && AcceptedEncodings.Contains("gzip"))
                {
                    gzipEnable = true;
                    Headers["Content-Encoding"] = "gzip";
                }
                if(IsChunkedAccepted)
                {
                    Headers["Transfer-Encoding"] = "chunked";
                }
                else
                {
                    Headers["Connection"] = "close";
                }
                SendHeaders();
            }
            else
            {
                throw new InvalidOperationException();
            }

            Stream stream;
            if(IsChunkedAccepted)
            {
                stream = new HttpWriteChunkedBodyStream(m_Output);
            }
            else
            {
                stream = new HttpResponseBodyStream(m_Output);
            }
            /* we never give out the original stream because Close is working recursively according to .NET specs */
            if (gzipEnable)
            {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            return stream;
        }

        public override Stream GetChunkedOutputStream()
        {
            if (IsChunkedAccepted)
            {
                if (!m_IsHeaderSent)
                {
                    Headers["Transfer-Encoding"] = "chunked";
                    SendHeaders();
                }
                else
                {
                    throw new InvalidOperationException();
                }

                return ResponseBody = new HttpWriteChunkedBodyStream(m_Output);
            }
            else
            {
                return GetOutputStream();
            }
        }

        public override void Dispose()
        {
            ResponseBody?.Dispose();
            Close();
            if (IsCloseConnection)
            {
                m_Output?.Dispose();
            }
        }
    }
}
