/*

ArribaSim is distributed under the terms of the
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
