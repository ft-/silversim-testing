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
using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class Http2Response : HttpResponse
    {
        private Http2Connection.Http2Stream m_Output;
        private bool m_IsHeaderSent;
        private Stream ResponseBody;

        public Http2Response(Http2Connection.Http2Stream output, HttpRequest request, HttpStatusCode statusCode, string statusDescription)
            : base(request, statusCode, statusDescription)
        {
            Headers["Content-Type"] = "text/html";
            m_Output = output;
            MajorVersion = request.MajorVersion;
            MinorVersion = request.MinorVersion;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        protected override void SendHeaders() => SendHeaders(false);

        private void SendHeaders(bool eos)
        {
            Headers[":status"] = ((uint)StatusCode).ToString();
            m_Output.SendHeaders(Headers, eos);
            m_IsHeaderSent = true;
        }

        public override void Close()
        {
            if (!m_IsHeaderSent)
            {
                Headers["Content-Length"] = "0";
                SendHeaders(true);
            }
            if (ResponseBody != null)
            {
                ResponseBody.Close();
                ResponseBody = null;
            }
            m_Output?.Flush();

            m_Output?.Close();
            throw new ConnectionCloseException();
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

            Http2Connection.Http2Stream s = m_Output;
            m_Output = null;
            return s;
        }

        public override Stream GetOutputStream(bool disableCompression = false)
        {
            bool gzipEnable = false;
            if (!m_IsHeaderSent)
            {
                Headers.Remove("Content-Length");
                if (!disableCompression && AcceptedEncodings != null && AcceptedEncodings.Contains("gzip"))
                {
                    gzipEnable = true;
                    Headers["Content-Encoding"] = "gzip";
                }
                SendHeaders();
            }
            else
            {
                throw new InvalidOperationException();
            }

            Stream stream = m_Output;
            if (gzipEnable)
            {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            m_Output = null;
            return stream;
        }

        public override Stream GetChunkedOutputStream()
        {
            return GetOutputStream();
        }

        public override void Dispose()
        {
            ResponseBody?.Dispose();
            Close();
            m_Output?.Dispose();
        }
    }
}