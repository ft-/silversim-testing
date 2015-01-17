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
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpRequest
    {
        #region Types
        public enum ConnectionModeEnum
        {
            Close,
            KeepAlive
        }
        #endregion

        #region Private Fields
        private Stream m_HttpStream;
        private Dictionary<string, string> m_Headers = new Dictionary<string, string>();
        #endregion

        #region Properties
        public uint MajorVersion { get; private set; }
        public uint MinorVersion { get; private set; }
        public string RawUrl { get; private set; }
        public string Method { get; private set; }
        public Stream Body { get; private set; }
        private HttpRequestBodyStream RawBody;
        public ConnectionModeEnum ConnectionMode { get; private set; }
        public HttpResponse Response { get; private set; }
        public string CallerIP { get; private set; }

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
                    return m_Headers["Content-Type"];
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
            if(Response == null)
            {
                BeginResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
            }
            Response.Close();
        }

        private string ReadHeaderLine()
        {
            int c;
            string headerLine = string.Empty;
            while((c = m_HttpStream.ReadByte()) != '\r')
            {
                if(c == -1)
                {
                    MajorVersion = 1;
                    MinorVersion = 1;
                    ConnectionMode = ConnectionModeEnum.Close;
                    HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                    res.Close();
                }
                headerLine += (char)c;
            }

            if(m_HttpStream.ReadByte() != '\n')
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = ConnectionModeEnum.Close;
                HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                res.Close();
            }

            return headerLine;
        }

        public HttpRequest(Stream httpStream, string callerIP, bool isBehindProxy)
        {
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
                ConnectionMode = ConnectionModeEnum.Close;
                HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                res.Close();
            }
            string[] version = requestData[2].Split('/');
            if(version.Length != 2)
            {
                ConnectionMode = ConnectionModeEnum.Close;
                HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                res.Close();
            }

            /* Check for version */
            if(version[0] != "HTTP")
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = ConnectionModeEnum.Close;
                HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                res.Close();
            }

            string[] versiondata = version[1].Split('.');
            if(versiondata.Length != 2)
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = ConnectionModeEnum.Close;
                HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                res.Close();
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
                ConnectionMode = ConnectionModeEnum.Close;
                HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                res.Close();
            }

            if(MajorVersion != 1)
            {
                MajorVersion = 1;
                MinorVersion = 1;
                ConnectionMode = ConnectionModeEnum.Close;
                HttpResponse res = BeginResponse(HttpStatusCode.HttpVersionNotSupported, "HTTP Version not supported");
                res.Close();
            }

            /* Configure connection mode default according to version */
            if(MinorVersion > 0)
            {
                ConnectionMode = ConnectionModeEnum.KeepAlive;
            }
            else
            {
                ConnectionMode = ConnectionModeEnum.Close;
            }

            Method = requestData[0];
            RawUrl = requestData[1];

            /* parse Headers */
            string lastHeader = string.Empty;
            while((headerLine = ReadHeaderLine()) != string.Empty)
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
                if(headerData.Length != 2)
                {
                    ConnectionMode = ConnectionModeEnum.Close;
                    HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                    res.Close();
                }
                else if(headerData[0].Trim() != headerData[0])
                {
                    ConnectionMode = ConnectionModeEnum.Close;
                    HttpResponse res = BeginResponse(HttpStatusCode.BadRequest, "Bad Request");
                    res.Close();
                }
                lastHeader = headerData[0];
                m_Headers[lastHeader] = headerData[1].Trim();
            }

            if(m_Headers.ContainsKey("Connection"))
            {
                if(m_Headers["Connection"] == "keep-alive")
                {
                    ConnectionMode = ConnectionModeEnum.KeepAlive;
                }
                else if(m_Headers["Connection"] == "close")
                {
                    ConnectionMode = ConnectionModeEnum.Close;
                }
            }

            if(ContainsHeader("Content-Length"))
            {
                /* there is a body */
                RawBody = new HttpRequestBodyStream(m_HttpStream, long.Parse(m_Headers["Content-Length"]));
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
                        else if(transferEncoding == "chunked")
                        {
                            Body = new HttpRequestChunkedBodyStream(Body);
                        }
                        else
                        {
                            ConnectionMode = ConnectionModeEnum.Close;
                            HttpResponse res = BeginResponse(HttpStatusCode.NotImplemented, "Transfer-Encoding " + transferEncoding + " not implemented");
                            res.Close();
                        }
                    }
                }

                string contentEncoding = "";
                if(m_Headers.ContainsKey("Content-Encoding"))
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
                /* following word is a synomyn for no-encoding so we use it for code simplification */
                else if(contentEncoding == "identity")
                {

                }
                else
                {
                    ConnectionMode = ConnectionModeEnum.Close;
                    HttpResponse res = BeginResponse(HttpStatusCode.NotImplemented, "Content-Encoding not accepted");
                    res.Close();
                }
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
                            ConnectionMode = ConnectionModeEnum.Close;
                        }
                        Body = new GZipStream(Body, CompressionMode.Decompress);
                    }
                    else if (transferEncoding == "chunked")
                    {
                        HaveChunkedInFront = true;
                        Body = new HttpRequestChunkedBodyStream(Body);
                    }
                    else
                    {
                        ConnectionMode = ConnectionModeEnum.Close;
                        HttpResponse res = BeginResponse(HttpStatusCode.NotImplemented, "Transfer-Encoding " + transferEncoding + " not implemented");
                        res.Close();
                    }
                }
            }

            if (m_Headers.ContainsKey("X-Forwarded-For") && isBehindProxy)
            {
                CallerIP = m_Headers["X-Forwarded-For"];
            }
            else
            {
                CallerIP = callerIP;
            }
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

        public HttpResponse BeginResponse(string contentType)
        {
            HttpResponse res = BeginResponse();
            res.ContentType = contentType;
            return res;
        }

        public void ErrorResponse(HttpStatusCode statuscode, string statusDescription)
        {
            BeginResponse(statuscode, statusDescription).Close();
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
    }
}
