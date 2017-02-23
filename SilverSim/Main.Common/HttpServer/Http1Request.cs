// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;
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
        Stream m_HttpStream;
        #endregion

        #region Properties
        private HttpRequestBodyStream RawBody;
        #endregion

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
            StringBuilder headerLine = new StringBuilder();
            while ((c = m_HttpStream.ReadByte()) != '\r')
            {
                if (c == -1)
                {
                    MajorVersion = 1;
                    MinorVersion = 1;
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Http1Request(Stream httpStream, string callerIP, bool isBehindProxy, bool isSsl)
            : base(callerIP, isBehindProxy, isSsl)
        {
            m_HttpStream = httpStream;
            Body = null;
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

            if (m_Headers.ContainsKey("connection"))
            {
                if (m_Headers["connection"] == "keep-alive")
                {
                    ConnectionMode = HttpConnectionMode.KeepAlive;
                }
                else if (m_Headers["connection"] == "close")
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
            if (m_Headers.ContainsKey("content-length"))
            {
                /* there is a body */
                long contentLength;
                if (!long.TryParse(m_Headers["content-length"], out contentLength))
                {
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                RawBody = new HttpRequestBodyStream(m_HttpStream, contentLength, Expect100Continue);
                Body = RawBody;

                if (m_Headers.ContainsKey("transfer-encoding"))
                {
                    string[] transferEncodings = m_Headers["transfer-encoding"].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string transferEncoding in transferEncodings)
                    {
                        if (transferEncoding == "gzip" || transferEncoding == "x-gzip")
                        {
                            Body = new GZipStream(Body, CompressionMode.Decompress);
                        }
                        else if (transferEncoding == "deflate")
                        {
                            Body = new DeflateStream(Body, CompressionMode.Decompress);
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
                bool HaveChunkedInFront = false;
                Body = m_HttpStream;
                string[] transferEncodings = m_Headers["transfer-encoding"].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string transferEncoding in transferEncodings)
                {
                    if (transferEncoding == "gzip" || transferEncoding == "x-gzip")
                    {
                        if (!HaveChunkedInFront)
                        {
                            ConnectionMode = HttpConnectionMode.Close;
                        }
                        Body = new GZipStream(Body, CompressionMode.Decompress);
                    }
                    else if (transferEncoding == "chunked")
                    {
                        HaveChunkedInFront = true;
                        Body = new HttpReadChunkedBodyStream(Body);
                    }
                    else
                    {
                        ConnectionMode = HttpConnectionMode.Close;
                        ErrorResponse(HttpStatusCode.NotImplemented, "Transfer-Encoding " + transferEncoding + " not implemented");
                        throw new InvalidDataException();
                    }
                }
                if (Expect100Continue)
                {
                    byte[] b = Encoding.ASCII.GetBytes("HTTP/1.0 100 Continue\r\n\r\n");
                    m_HttpStream.Write(b, 0, b.Length);
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
                    Body = new GZipStream(Body, CompressionMode.Decompress);
                }
                else if (contentEncoding == "deflate")
                {
                    Body = new DeflateStream(Body, CompressionMode.Decompress);
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
            if (Body != null)
            {
                Body.Close();
                Body = null;
            }
            if (RawBody != null)
            {
                RawBody.Close();
                RawBody = null;
            }
            return Response = new Http1Response(m_HttpStream, this, HttpStatusCode.OK, "OK");
        }

        public override HttpResponse BeginResponse(HttpStatusCode statuscode, string statusDescription)
        {
            if (Body != null)
            {
                Body.Close();
                Body = null;
            }
            if (RawBody != null)
            {
                RawBody.Close();
                RawBody = null;
            }
            return Response = new Http1Response(m_HttpStream, this, statuscode, statusDescription);
        }

        public override HttpResponse BeginChunkedResponse()
        {
            if (Body != null)
            {
                Body.Close();
                Body = null;
            }
            if (RawBody != null)
            {
                RawBody.Close();
                RawBody = null;
            }
            Response = new Http1Response(m_HttpStream, this, HttpStatusCode.OK, "OK");
            Response.Headers["Transfer-Encoding"] = "chunked";
            return Response;
        }

        public override HttpResponse BeginChunkedResponse(string contentType)
        {
            if (Body != null)
            {
                Body.Close();
                Body = null;
            }
            if (RawBody != null)
            {
                RawBody.Close();
                RawBody = null;
            }
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

            string websocketkeyuuid;
            websocketkeyuuid = m_Headers["sec-websocket-key"].Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] websocketacceptdata = websocketkeyuuid.ToUTF8Bytes();
            string websocketaccept;
            using (SHA1 sha1 = SHA1.Create())
            {
                websocketaccept = Convert.ToBase64String(sha1.ComputeHash(websocketacceptdata));
            }
            SetConnectionClose();

            using (MemoryStream ms = new MemoryStream())
            {
                using (TextWriter w = ms.UTF8StreamWriter())
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
