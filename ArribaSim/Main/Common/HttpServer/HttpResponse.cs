using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ArribaSim.Main.Common.HttpServer
{
    public class HttpResponse
    {
        #region Connection Close Signalling
        public class ConnectionCloseException : Exception
        {
            public ConnectionCloseException()
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
        private HttpResponseBodyStream ResponseBody = null;

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
        }

        private void SendHeaders()
        {
            MemoryStream ms = new MemoryStream();
            using (TextWriter w = new StreamWriter(ms))
            {
                w.Write(string.Format("HTTP/{0}.{1} {2} {3}\r\n", MajorVersion, MinorVersion, (uint)StatusCode, StatusDescription.Replace("\n", "").Replace("\r", "")));
                foreach(KeyValuePair<string, string> kvp in Headers)
                {
                    w.Write(string.Format("{0}: {1}\r\n", kvp.Key.Replace("\r", "").Replace("\n", ""), kvp.Value.Replace("\r", "").Replace("\n", "")));
                }
                w.Write("\r\n");
                w.Flush();
                m_Output.Write(ms.GetBuffer(), 0, (int)ms.Length);
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
            }

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

        public Stream GetOutputStream()
        {
            if (!m_IsHeaderSent)
            {
                IsCloseConnection = true;
                Headers["Connection"] = "close";
                SendHeaders();
            }
            else
            {
                throw new InvalidOperationException();
            }

            return m_Output;
        }
    }
}
