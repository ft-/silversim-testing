// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Text;
using SilverSim.Http;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class HttpResponse : IDisposable
    {
        #region Connection Close Signalling
        [Serializable]
        public class ConnectionCloseException : Exception
        {
            public ConnectionCloseException()
            {

            }

            public ConnectionCloseException(string message)
                : base(message)
            {

            }

            protected ConnectionCloseException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public ConnectionCloseException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        [Serializable]
        public class DisconnectFromThreadException : Exception
        {
            public DisconnectFromThreadException()
            {

            }

            public DisconnectFromThreadException(string message)
                : base(message)
            {

            }

            protected DisconnectFromThreadException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public DisconnectFromThreadException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }
        #endregion

        public readonly Dictionary<string, string> Headers = new Dictionary<string,string>();
        readonly Stream m_Output;
        public HttpStatusCode StatusCode;
        public string StatusDescription;
        public uint MajorVersion;
        public uint MinorVersion;
        public bool IsCloseConnection { get; private set; }
        private bool m_IsHeaderSent;
        private Stream ResponseBody;
        readonly bool IsChunkedAccepted;
        readonly List<string> AcceptedEncodings;
        private static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public string ContentType
        {
            get
            {
                return Headers["Content-Type"];
            }
            set
            {
                Headers["Content-Type"] = value;
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule")]
        public HttpResponse(Stream output, HttpRequest request, HttpStatusCode statusCode, string statusDescription)
        {
            Headers["Content-Type"] = "text/html";
            m_Output = output;
            MajorVersion = request.MajorVersion;
            MinorVersion = request.MinorVersion;
            IsCloseConnection = HttpConnectionMode.Close == request.ConnectionMode;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            IsChunkedAccepted = request.ContainsHeader("TE");
            if(request.ContainsHeader("Accept"))
            {
                string[] parts = request["Accept"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                AcceptedEncodings = new List<string>();
                foreach(string part in parts)
                {
                    AcceptedEncodings.Add(part.Trim());
                }
            }
        }

        private void SendHeaders()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (TextWriter w = new StreamWriter(ms, UTF8NoBOM))
                {
                    w.Write(string.Format("HTTP/{0}.{1} {2} {3}\r\n", MajorVersion, MinorVersion, (uint)StatusCode, StatusDescription.Replace("\n", string.Empty).Replace("\r", string.Empty)));
                    foreach (KeyValuePair<string, string> kvp in Headers)
                    {
                        w.Write(string.Format("{0}: {1}\r\n", kvp.Key.Replace("\r", string.Empty).Replace("\n", string.Empty), kvp.Value.Replace("\r", string.Empty).Replace("\n", string.Empty)));
                    }
                    w.Write("\r\n");
                    w.Flush();
                    m_Output.Write(ms.GetBuffer(), 0, (int)ms.Length);
                    m_Output.Flush();
                }
            }
            m_IsHeaderSent = true;
        }

        public void Close()
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

            if(IsCloseConnection)
            {
                throw new ConnectionCloseException();
            }
        }

        public Stream GetOutputStream(long contentLength)
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

        public Stream GetOutputStream(bool disableCompression = false)
        {
            bool gzipEnable = false;
            if (!m_IsHeaderSent)
            {
                IsCloseConnection = true;
                Headers["Connection"] = "close";
                if(!disableCompression && AcceptedEncodings != null && AcceptedEncodings.Contains("gzip"))
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

            /* we never give out the original stream because Close is working recursively according to .NET specs */
            if(gzipEnable)
            {
                return new GZipStream(new HttpResponseBodyStream(m_Output), CompressionMode.Compress);
            }
            return new HttpResponseBodyStream(m_Output);
        }

        public Stream GetChunkedOutputStream()
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

        public void Dispose()
        {
            if (null != ResponseBody)
            {
                ResponseBody.Dispose();
            }
            Close();
            if (null != m_Output)
            {
                m_Output.Dispose();
            }
        }
    }
}
