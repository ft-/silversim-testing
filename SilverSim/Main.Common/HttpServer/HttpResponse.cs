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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

namespace SilverSim.Main.Common.HttpServer
{
    public abstract class HttpResponse : IDisposable
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
        public HttpStatusCode StatusCode;
        public string StatusDescription;
        public uint MajorVersion;
        public uint MinorVersion;
        public bool IsCloseConnection { get; protected set; }
        protected readonly List<string> AcceptedEncodings;

        public string ContentType
        {
            get { return Headers["Content-Type"]; }

            set { Headers["Content-Type"] = value; }
        }

        protected HttpResponse(HttpRequest request, HttpStatusCode statusCode, string statusDescription)
        {
            Headers["Content-Type"] = "text/html";
            MajorVersion = request.MajorVersion;
            MinorVersion = request.MinorVersion;
            IsCloseConnection = HttpConnectionMode.Close == request.ConnectionMode;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
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

        protected abstract void SendHeaders();

        public abstract void Close();

        public abstract Stream GetOutputStream(long contentLength);

        public abstract Stream GetOutputStream(bool disableCompression = false);

        public abstract Stream GetChunkedOutputStream();

        public abstract void Dispose();
    }
}
