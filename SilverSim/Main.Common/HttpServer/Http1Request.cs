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
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class Http1Request : HttpRequest
    {
        #region Private Fields
        private Stream m_HttpStream;
        private HttpRequestBodyStream RawBody;

        private Stream m_Body;
        #endregion

        public override Stream Body
        {
            get
            {
                if(Expect100Continue && m_Body != null)
                {
                    byte[] b = Encoding.ASCII.GetBytes("HTTP/1.0 100 Continue\r\n\r\n");
                    m_HttpStream.Write(b, 0, b.Length);
                    Expect100Continue = false;
                }
                return m_Body;
            }
        }

        public override bool HasRequestBody => m_Body != null;

        public override void Close()
        {
            if (m_HttpStream == null)
            {
                return;
            }
            if (Response == null)
            {
                using (BeginResponse(HttpStatusCode.InternalServerError, "Internal Server Error"))
                {
                    /* nothing additional to do here */
                }
            }
            Response.Close();
        }

        public override void SetConnectionClose()
        {
            ConnectionMode = HttpConnectionMode.Close;
        }

        private string ReadHeaderLine()
        {
            int c;
            var headerLine = new StringBuilder();
            while ((c = m_HttpStream.ReadByte()) != '\r')
            {
                if (c == -1)
                {
                    MajorVersion = 1;
                    MinorVersion = 1;
                    ConnectionMode = HttpConnectionMode.Close;
                    if (headerLine.Length != 0)
                    {
                        ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    }
                    throw new HttpResponse.ConnectionCloseException();
                }
                headerLine.Append((char)c);
            }

            if (m_HttpStream.ReadByte() != '\n')
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }

            return headerLine.ToString();
        }

        public Http1Request(Stream httpStream, string callerIP, bool isBehindProxy, bool isSsl)
            : base(isSsl)
        {
            m_HttpStream = httpStream;
            m_Body = null;
            string headerLine;
            string requestInfo = ReadHeaderLine();

            /* Parse request line */
            string[] requestData = requestInfo.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (requestData.Length != 3)
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }
            string[] version = requestData[2].Split('/');
            if (version.Length != 2)
            {
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }

            /* Check for version */
            if (version[0] != "HTTP")
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }

            string[] versiondata = version[1].Split('.');
            if (versiondata.Length != 2)
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }

            /* Check whether we know that request version */
            try
            {
                MajorVersion = uint.Parse(versiondata[0]);
                MinorVersion = uint.Parse(versiondata[1]);
            }
            catch
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }

            if(MajorVersion == 2)
            {
                Method = requestData[0];
                RawUrl = requestData[1];

                /* this is HTTP/2 client preface */
                while (ReadHeaderLine().Length != 0)
                {
                    /* skip headers */
                }
                if(ReadHeaderLine() != "SM")
                {
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                if(ReadHeaderLine().Length != 0)
                {
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                CallerIP = (m_Headers.ContainsKey("x-forwarded-for") && isBehindProxy) ?
                    m_Headers["x-forwarded-for"] :
                    callerIP;
                return;
            }

            if (MajorVersion != 1)
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.HttpVersionNotSupported, "HTTP Version not supported");
                throw new InvalidDataException();
            }

            /* Configure connection mode default according to version */
            ConnectionMode = MinorVersion > 0 ? HttpConnectionMode.KeepAlive : HttpConnectionMode.Close;

            Method = requestData[0];
            RawUrl = requestData[1];

            /* parse Headers */
            string lastHeader = string.Empty;
            while ((headerLine = ReadHeaderLine()).Length != 0)
            {
                if (m_Headers.Count == 0)
                {
                    /* we have to trim first header line as per RFC7230 when it starts with whitespace */
                    headerLine = headerLine.TrimStart(new char[] { ' ', '\t' });
                }
                /* a white space designates a continuation , RFC7230 deprecates is use for anything else than Content-Type but we stay more permissive here */
                else if (char.IsWhiteSpace(headerLine[0]))
                {
                    m_Headers[lastHeader] += headerLine.Trim();
                    continue;
                }

                string[] headerData = headerLine.Split(new char[] { ':' }, 2);
                if (headerData.Length != 2 || headerData[0].Trim() != headerData[0])
                {
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                lastHeader = headerData[0].ToLowerInvariant();
                m_Headers[lastHeader] = headerData[1].Trim();
            }

            string connectionfield;
            if(TryGetHeader("connection", out connectionfield))
            {
                if (connectionfield == "keep-alive")
                {
                    ConnectionMode = HttpConnectionMode.KeepAlive;
                }
                else if (connectionfield == "close")
                {
                    ConnectionMode = HttpConnectionMode.Close;
                }
            }

            Expect100Continue = false;
            if (m_Headers.ContainsKey("expect") &&
                m_Headers["expect"] == "100-continue")
            {
                Expect100Continue = true;
            }

            bool havePostData = false;
            string upgradeToken;
            bool isH2CUpgrade = !isSsl && m_Headers.TryGetValue("upgrade", out upgradeToken) && upgradeToken == "h2c" &&
                m_Headers.ContainsKey("http2-settings");

            bool hasContentLength = m_Headers.ContainsKey("content-length");
            bool hasH2cRequestBody = isH2CUpgrade && (hasContentLength || m_Headers.ContainsKey("transfer-encoding"));

            IsH2CUpgradableAfterReadingBody = isH2CUpgrade && hasH2cRequestBody && !Expect100Continue && hasContentLength;

            if (isH2CUpgrade && (!hasH2cRequestBody || Expect100Continue))
            {
                IsH2CUpgradable = true;
                /* skip over post handling */
            }
            else if (hasContentLength)
            {
                /* there is a body */
                long contentLength;
                if (!long.TryParse(m_Headers["content-length"], out contentLength))
                {
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                if(IsH2CUpgradableAfterReadingBody && contentLength > 65536)
                {
                    IsH2CUpgradableAfterReadingBody = false;
                }
                RawBody = new HttpRequestBodyStream(m_HttpStream, contentLength);
                m_Body = RawBody;

                if (m_Headers.ContainsKey("transfer-encoding"))
                {
                    foreach (string transferEncoding in m_Headers["transfer-encoding"].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (transferEncoding == "gzip" || transferEncoding == "x-gzip")
                        {
                            m_Body = new GZipStream(m_Body, CompressionMode.Decompress);
                        }
                        else if (transferEncoding == "deflate")
                        {
                            m_Body = new DeflateStream(m_Body, CompressionMode.Decompress);
                        }
                        else
                        {
                            ConnectionMode = HttpConnectionMode.Close;
                            ErrorResponse(HttpStatusCode.NotImplemented, "Transfer-Encoding " + transferEncoding + " not implemented");
                            throw new InvalidDataException();
                        }
                    }
                }

                havePostData = true;
            }
            else if (m_Headers.ContainsKey("transfer-encoding"))
            {
                IsH2CUpgradableAfterReadingBody = false;
                bool HaveChunkedInFront = false;
                m_Body = m_HttpStream;
                foreach (string transferEncoding in m_Headers["transfer-encoding"].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (transferEncoding == "gzip" || transferEncoding == "x-gzip")
                    {
                        if (!HaveChunkedInFront)
                        {
                            ConnectionMode = HttpConnectionMode.Close;
                        }
                        m_Body = new GZipStream(m_Body, CompressionMode.Decompress);
                    }
                    else if (transferEncoding == "chunked")
                    {
                        HaveChunkedInFront = true;
                        m_Body = new HttpReadChunkedBodyStream(m_Body);
                    }
                    else
                    {
                        ConnectionMode = HttpConnectionMode.Close;
                        ErrorResponse(HttpStatusCode.NotImplemented, "Transfer-Encoding " + transferEncoding + " not implemented");
                        throw new InvalidDataException();
                    }
                }

                havePostData = true;
            }

            if (havePostData)
            {
                string contentEncoding = string.Empty;
                if (m_Headers.ContainsKey("content-encoding"))
                {
                    contentEncoding = m_Headers["content-encoding"];
                }
                else if (m_Headers.ContainsKey("x-content-encoding"))
                {
                    contentEncoding = m_Headers["x-content-encoding"];
                }
                else
                {
                    contentEncoding = "identity";
                }

                /* check for gzip encoding */
                if (contentEncoding == "gzip" || contentEncoding == "x-gzip") /* x-gzip is deprecated as per RFC7230 but better accept it if sent */
                {
                    m_Body = new GZipStream(m_Body, CompressionMode.Decompress);
                }
                else if (contentEncoding == "deflate")
                {
                    m_Body = new DeflateStream(m_Body, CompressionMode.Decompress);
                }
                else if (contentEncoding == "identity")
                {
                    /* word is a synomyn for no-encoding so we use it for code simplification */
                    /* no additional action required, identity is simply transfer as-is */
                }
                else
                {
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.NotImplemented, "Content-Encoding not accepted");
                    throw new InvalidDataException();
                }
            }

            CallerIP = (m_Headers.ContainsKey("x-forwarded-for") && isBehindProxy) ?
                m_Headers["x-forwarded-for"] :
                callerIP;
        }

        public override HttpResponse BeginResponse()
        {
            FinishRequestBody();
            return Response = new Http1Response(m_HttpStream, this, HttpStatusCode.OK, "OK");
        }

        private void FinishRequestBody()
        {
            if (m_Body != null)
            {
                if (Expect100Continue)
                {
                    m_Body.Dispose();
                }
                else
                {
                    m_Body.Close();
                }
                m_Body = null;
            }
            if (RawBody != null)
            {
                if (Expect100Continue)
                {
                    RawBody.Dispose();
                }
                else
                {
                    RawBody.Close();
                }
                RawBody = null;
            }
        }

        public override HttpResponse BeginResponse(HttpStatusCode statuscode, string statusDescription)
        {
            FinishRequestBody();
            return Response = new Http1Response(m_HttpStream, this, statuscode, statusDescription);
        }

        public override HttpResponse BeginChunkedResponse()
        {
            FinishRequestBody();
            Response = new Http1Response(m_HttpStream, this, HttpStatusCode.OK, "OK");
            Response.Headers["Transfer-Encoding"] = "chunked";
            return Response;
        }

        public override HttpResponse BeginChunkedResponse(string contentType)
        {
            FinishRequestBody();
            Response = new Http1Response(m_HttpStream, this, HttpStatusCode.OK, "OK");
            Response.Headers["Transfer-Encoding"] = "chunked";
            Response.Headers["Content-Type"] = contentType;
            return Response;
        }

        public override HttpWebSocket BeginWebSocket(string websocketprotocol = "")
        {
            Stream websocketStream;
            if (!IsWebSocket)
            {
                throw new NotAWebSocketRequestException();
            }

            string websocketkeyuuid = m_Headers["sec-websocket-key"].Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] websocketacceptdata = websocketkeyuuid.ToUTF8Bytes();
            string websocketaccept;
            using (var sha1 = SHA1.Create())
            {
                websocketaccept = Convert.ToBase64String(sha1.ComputeHash(websocketacceptdata));
            }
            SetConnectionClose();

            using (var ms = new MemoryStream())
            {
                using (var w = ms.UTF8StreamWriter())
                {
                    w.Write(string.Format("HTTP/{0}.{1} 101 Switching Protocols\r\n", MajorVersion, MinorVersion));
                    w.Write("Upgrade: websocket\r\nConnection: Upgrade\r\n");
                    w.Write(string.Format("Sec-WebSocket-Accept: {0}\r\n", websocketaccept));
                    if (!string.IsNullOrEmpty(websocketprotocol))
                    {
                        w.Write(string.Format("Sec-WebSocket-Protocol: {0}\r\n", websocketprotocol));
                    }
                    w.Write("\r\n");
                    w.Flush();
                    byte[] b = ms.ToArray();
                    m_HttpStream.Write(b, 0, b.Length);
                    m_HttpStream.Flush();
                }
            }
            websocketStream = m_HttpStream;
            m_HttpStream = null;
            return new HttpWebSocket(websocketStream);
        }
    }
}
