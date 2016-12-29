// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SilverSim.Main.Common.HttpServer
{
    public enum HttpConnectionMode
    {
        Close,
        KeepAlive
    }

    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    public sealed class HttpRequest
    {
        #region Private Fields
        Stream m_HttpStream;
        readonly Dictionary<string, string> m_Headers = new Dictionary<string, string>();
        #endregion

        #region Properties
        public uint MajorVersion { get; private set; }
        public uint MinorVersion { get; private set; }
        public string RawUrl { get; private set; }
        public string Method { get; private set; }
        public Stream Body { get; private set; }
        private HttpRequestBodyStream RawBody;
        public HttpConnectionMode ConnectionMode { get; private set; }
        public HttpResponse Response { get; private set; }
        public string CallerIP { get; private set; }
        public bool Expect100Continue { get; private set; }
        public bool IsSsl { get; private set; }

        public bool IsChunkedAccepted
        {
            get
            {
                return ContainsHeader("TE");
            }
        }

        public string this[string fieldName]
        {
            get
            {
                return m_Headers[fieldName];
            }
            set
            {
                m_Headers[fieldName] = value;
            }
        }

        public bool ContainsHeader(string fieldName)
        {
            return m_Headers.ContainsKey(fieldName);
        }

        public string ContentType
        {
            get
            {
                if (m_Headers.ContainsKey("Content-Type"))
                {
                    string contentType = m_Headers["Content-Type"];
                    int semi = contentType.IndexOf(';');
                    return semi >= 0 ? contentType.Substring(0, semi).Trim() : contentType;
                }
                return string.Empty;
            }
            set
            {
                m_Headers["Content-Type"] = value;
            }
        }
        #endregion

        public void Close()
        {
            if(m_HttpStream == null)
            {
                return;
            }
            if (Response == null)
            {
                using(BeginResponse(HttpStatusCode.InternalServerError, "Internal Server Error"))
                {
                    /* nothing additional to do here */
                }
            }
            Response.Close();
        }

        public void SetConnectionClose()
        {
            ConnectionMode = HttpConnectionMode.Close;
        }

        private string ReadHeaderLine()
        {
            int c;
            StringBuilder headerLine = new StringBuilder();
            while((c = m_HttpStream.ReadByte()) != '\r')
            {
                if(c == -1)
                {
                    MajorVersion = 1;
                    MinorVersion = 1;
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                headerLine.Append((char)c);
            }

            if(m_HttpStream.ReadByte() != '\n')
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
        public HttpRequest(Stream httpStream, string callerIP, bool isBehindProxy, bool isSsl)
        {
            IsSsl = isSsl;
            m_HttpStream = httpStream;
            Body = null;
            string headerLine;
            string requestInfo = ReadHeaderLine();

            /* Parse request line */
            string[] requestData = requestInfo.Split(new char[]{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
            if(requestData.Length != 3)
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }
            string[] version = requestData[2].Split('/');
            if(version.Length != 2)
            {
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }

            /* Check for version */
            if(version[0] != "HTTP")
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = HttpConnectionMode.Close;
                ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                throw new InvalidDataException();
            }

            string[] versiondata = version[1].Split('.');
            if(versiondata.Length != 2)
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

            if(MajorVersion != 1)
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
            while((headerLine = ReadHeaderLine()).Length != 0)
            {
                if(m_Headers.Count == 0)
                {
                    /* we have to trim first header line as per RFC7230 when it starts with whitespace */
                    headerLine = headerLine.TrimStart(new char[] { ' ', '\t' });
                }
                /* a white space designates a continuation , RFC7230 deprecates is use for anything else than Content-Type but we stay more permissive here */
                else if(char.IsWhiteSpace(headerLine[0]))
                {
                    m_Headers[lastHeader] += headerLine.Trim();
                    continue;
                }

                string[] headerData = headerLine.Split(new char[]{':'}, 2);
                if (headerData.Length != 2 || headerData[0].Trim() != headerData[0])
                {
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                lastHeader = headerData[0];
                m_Headers[lastHeader] = headerData[1].Trim();
            }

            if(m_Headers.ContainsKey("Connection"))
            {
                if(m_Headers["Connection"] == "keep-alive")
                {
                    ConnectionMode = HttpConnectionMode.KeepAlive;
                }
                else if(m_Headers["Connection"] == "close")
                {
                    ConnectionMode = HttpConnectionMode.Close;
                }
            }

            Expect100Continue = false;
            if (ContainsHeader("Expect") &&
                m_Headers["Expect"] == "100-continue")
            {
                Expect100Continue = true;
            }

            bool havePostData = false;
            if (ContainsHeader("Content-Length"))
            {
                /* there is a body */
                long contentLength;
                if(!long.TryParse(m_Headers["Content-Length"], out contentLength))
                {
                    ConnectionMode = HttpConnectionMode.Close;
                    ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    throw new InvalidDataException();
                }
                RawBody = new HttpRequestBodyStream(m_HttpStream, contentLength, Expect100Continue);
                Body = RawBody;

                if(ContainsHeader("Transfer-Encoding"))
                {
                    string[] transferEncodings = this["Transfer-Encoding"].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string transferEncoding in transferEncodings)
                    {
                        if(transferEncoding == "gzip" || transferEncoding == "x-gzip")
                        {
                            Body = new GZipStream(Body, CompressionMode.Decompress);
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
            else if(ContainsHeader("Transfer-Encoding"))
            {
                bool HaveChunkedInFront = false;
                Body = m_HttpStream;
                string[] transferEncodings = this["Transfer-Encoding"].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

            if(havePostData)
            {
                string contentEncoding = string.Empty;
                if (m_Headers.ContainsKey("Content-Encoding"))
                {
                    contentEncoding = m_Headers["Content-Encoding"];
                }
                else if (m_Headers.ContainsKey("X-Content-Encoding"))
                {
                    contentEncoding = m_Headers["X-Content-Encoding"];
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

            CallerIP = (m_Headers.ContainsKey("X-Forwarded-For") && isBehindProxy) ?
                m_Headers["X-Forwarded-For"] : 
                callerIP;
        }

        public HttpResponse BeginResponse()
        {
            if(Body != null)
            {
                Body.Close();
                Body = null;
            }
            if (RawBody != null)
            {
                RawBody.Close();
                RawBody = null;
            }
            return Response = new HttpResponse(m_HttpStream, this, HttpStatusCode.OK, "OK");
        }

        public void EmptyResponse(string contentType = "text/plain")
        {
            BeginResponse(contentType).Dispose();
        }

        public HttpResponse BeginResponse(string contentType)
        {
            HttpResponse res = BeginResponse();
            res.ContentType = contentType;
            return res;
        }

        public HttpResponse BeginResponse(HttpStatusCode statuscode, string statusDescription, string contentType)
        {
            HttpResponse res = BeginResponse(statuscode, statusDescription);
            res.ContentType = contentType;
            return res;
        }

        public void ErrorResponse(HttpStatusCode statuscode, string statusDescription)
        {
            using(HttpResponse res = BeginResponse(statuscode, statusDescription))
            {
                res.ContentType = "text/plain";
            }
        }

        public HttpResponse BeginResponse(HttpStatusCode statuscode, string statusDescription)
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
            return Response = new HttpResponse(m_HttpStream, this, statuscode, statusDescription);
        }

        public HttpResponse BeginChunkedResponse()
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
            Response = new HttpResponse(m_HttpStream, this, HttpStatusCode.OK, "OK");
            Response.Headers["Transfer-Encoding"] = "chunked";
            return Response;
        }

        public HttpResponse BeginChunkedResponse(string contentType)
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
            Response = new HttpResponse(m_HttpStream, this, HttpStatusCode.OK, "OK");
            Response.Headers["Transfer-Encoding"] = "chunked";
            Response.Headers["Content-Type"] = contentType;
            return Response;
        }

        public bool IsWebSocket
        {
            get
            {
                if(Method != "GET")
                {
                    return false;
                }
                if(!(MajorVersion > 1 || (MajorVersion == 1 && MinorVersion >= 1)))
                {
                    return false;
                }

                string val;
                if(!m_Headers.TryGetValue("Upgrade", out val) || val.ToLower() != "websocket")
                {
                    return false;
                }

                if(!m_Headers.ContainsKey("Sec-WebSocket-Key"))
                {
                    return false;
                }
                if(!m_Headers.TryGetValue("Sec-WebSocket-Version", out val) || val != "13")
                {
                    return false;
                }

                /* Connection header is checked last */
                if (!m_Headers.TryGetValue("Connection", out val))
                {
                    return false;
                }

                foreach (string valitem in val.Split(','))
                {
                    if (valitem.ToLower().Trim() == "upgrade")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public HttpWebSocket BeginWebSocket(string websocketprotocol = "")
        {
            Stream websocketStream;
            if(!IsWebSocket)
            {
                throw new NotAWebSocketRequestException();
            }

            string websocketkeyuuid;
            websocketkeyuuid = m_Headers["Sec-WebSocket-Key"].Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
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
