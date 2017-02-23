// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
