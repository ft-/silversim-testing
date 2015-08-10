﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Text;
using SilverSim.Main.Common.Http;

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpResponse : IDisposable
    {
        #region Connection Close Signalling
        public class ConnectionCloseException : Exception
        {
            public ConnectionCloseException()
            {

            }
        }

        public class DisconnectFromThreadException : Exception
        {
            public DisconnectFromThreadException()
            {

            }
        }
        #endregion

        public readonly Dictionary<string, string> Headers = new Dictionary<string,string>();
        private Stream m_Output;
        public HttpStatusCode StatusCode;
        public string StatusDescription;
        public uint MajorVersion;
        public uint MinorVersion;
        public bool IsCloseConnection { get; private set; }
        private bool m_IsHeaderSent = false;
        private Stream ResponseBody = null;
        private bool IsChunkedAccepted = false;
        private List<string> AcceptedEncodings = null;
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

        public HttpResponse(Stream output, HttpRequest request, HttpStatusCode statusCode, string statusDescription)
        {
            Headers["Content-Type"] = "text/html";
            m_Output = output;
            MajorVersion = request.MajorVersion;
            MinorVersion = request.MinorVersion;
            IsCloseConnection = HttpRequest.ConnectionModeEnum.Close == request.ConnectionMode;
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
            MemoryStream ms = new MemoryStream();
            using (TextWriter w = new StreamWriter(ms, UTF8NoBOM))
            {
                w.Write(string.Format("HTTP/{0}.{1} {2} {3}\r\n", MajorVersion, MinorVersion, (uint)StatusCode, StatusDescription.Replace("\n", "").Replace("\r", "")));
                foreach (KeyValuePair<string, string> kvp in Headers)
                {
                    w.Write(string.Format("{0}: {1}\r\n", kvp.Key.Replace("\r", "").Replace("\n", ""), kvp.Value.Replace("\r", "").Replace("\n", "")));
                }
                w.Write("\r\n");
                w.Flush();
                m_Output.Write(ms.GetBuffer(), 0, (int)ms.Length);
                m_Output.Flush();
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
            Close();
        }
    }
}
