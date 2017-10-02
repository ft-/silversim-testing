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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    public class Http2Request : HttpRequest
    {
        private Stream m_Body;
        public override Stream Body
        {
            get
            {
                if (Expect100Continue && m_Body != null)
                {
                    m_Stream.SendHeaders(new Dictionary<string, string>
                    {
                        [":status"] = "100"
                    });
                    Expect100Continue = false;
                }

                return m_Body;
            }
        }

        public override bool HasRequestBody => m_Body != null;

        Http2Connection.Http2Stream m_Stream;

        public Http2Request(Http2Connection.Http2Stream stream, string callerIP, bool isBehindProxy, bool isSsl, HttpRequest upgradeReq = null, Stream upgradeBody = null)
            : base(isSsl)
        {
            MajorVersion = 2;
            MinorVersion = 0;
            m_Stream = stream;
            if (upgradeReq == null)
            {
                foreach (KeyValuePair<string, string> kvp in m_Stream.ReceiveHeaders())
                {
                    m_Headers[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                foreach(KeyValuePair<string, string> kvp in upgradeReq.m_Headers)
                {
                    m_Headers[kvp.Key] = kvp.Value;
                }
                m_Headers[":method"] = upgradeReq.Method;
                m_Headers[":path"] = upgradeReq.RawUrl;
            }

            string value;
            if(!m_Headers.TryGetValue(":method", out value))
            {
                m_Stream.SendRstStream(Http2Connection.Http2ErrorCode.ProtocolError);
                throw new InvalidDataException();
            }
            Method = value;
            if (!m_Headers.TryGetValue(":path", out value))
            {
                m_Stream.SendRstStream(Http2Connection.Http2ErrorCode.ProtocolError);
                throw new InvalidDataException();
            }
            RawUrl = value;
            ConnectionMode = HttpConnectionMode.Close;

            Expect100Continue = false;
            if (m_Headers.ContainsKey("expect") &&
                m_Headers["expect"] == "100-continue")
            {
                Expect100Continue = true;
            }

            bool havePostData = false;
            if(upgradeReq != null && upgradeReq.IsH2CUpgradableAfterReadingBody)
            {
                /* skip following postdata handling here since it is already decoded */
                m_Body = upgradeBody;
            }
            else if (m_Headers.ContainsKey("content-length") || m_Headers.ContainsKey("content-type") ||
                m_Headers.ContainsKey("transfer-encoding"))
            {
                /* there is a body */
                m_Body = new RequestBodyStream(this);

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

        public override HttpResponse BeginChunkedResponse() =>
            BeginResponse(HttpStatusCode.OK, "OK");

        private void FinishRequestBody()
        {
            if (m_Body != null && !Expect100Continue)
            {
                if (!m_Stream.HaveReceivedEoS)
                {
                    var postbuf = new byte[10240];
                    while (!m_Stream.HaveReceivedEoS)
                    {
                        m_Stream.Read(postbuf, 0, postbuf.Length);
                    }
                }
                m_Body = null;
            }
        }

        public override HttpResponse BeginChunkedResponse(string contentType)
        {
            HttpResponse res = BeginResponse(HttpStatusCode.OK, "OK");
            res.ContentType = ContentType;
            return res;
        }

        public override HttpResponse BeginResponse()
            => BeginResponse(HttpStatusCode.OK, "OK");

        public override HttpResponse BeginResponse(HttpStatusCode statuscode, string statusDescription)
        {
            FinishRequestBody();
            var res = new Http2Response(m_Stream, this, statuscode, statusDescription);
            m_Stream = null;
            return res;
        }

        public override HttpWebSocket BeginWebSocket(string websocketprotocol = "")
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
        }

        public override void SetConnectionClose()
        {
        }

        public class RequestBodyStream : Stream
        {
            Http2Request m_Req;
            public RequestBodyStream(Http2Request req)
            {
                m_Req = req;
            }

            protected override void Dispose(bool disposing)
            {
                var buf = new byte[10240];
                while(!m_Req.m_Stream.HaveReceivedEoS)
                {
                    m_Req.m_Stream.Read(buf, 0, 10240);
                }
                base.Dispose(disposing);
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count) =>
                m_Req.m_Stream.Read(buffer, offset, count);

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}