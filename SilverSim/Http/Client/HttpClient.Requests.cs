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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        public class Request
        {
            public string Method = "GET";
            public string Url;
            public IDictionary<string, string> GetValues;
            public string RequestContentType;
            public int RequestContentLength;
            public byte[] RequestBody;
            public Action<Stream> RequestBodyDelegate;
            public bool IsCompressed;
            public int TimeoutMs = 20000;
            public ConnectionModeEnum ConnectionMode = ConnectionReuse;
            public IDictionary<string, string> Headers;
            public bool Expect100Continue;
            public bool UseChunkedEncoding;
            public int Expect100ContinueMinSize = 8192;

            public X509CertificateCollection ClientCertificates;
            public SslProtocols EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            public bool CheckCertificateRevocation = true;
            public IHttpAuthorization Authorization;
            public RemoteCertificateValidationCallback RemoteCertificateValidationCallback;

            public Request(string url)
            {
                Url = url;
            }
        }

        public class Post : Request
        {
            public Post(string url)
                : base(url)
            {
                Method = "POST";
            }

            public Post(string url, IDictionary<string, string> postValues)
                : base(url)
            {
                RequestBody = BuildQueryString(postValues).ToUTF8Bytes();
                RequestContentType = "application/x-www-form-urlencoded";
                Method = "POST";
            }

            public Post(string url, string contenttype, string body)
                : base(url)
            {
                RequestBody = body.ToUTF8Bytes();
                RequestContentType = contenttype;
                Method = "POST";
            }

            public Post(string url, string contenttype, byte[] body)
                : base(url)
            {
                RequestBody = body;
                RequestContentType = contenttype;
                Method = "POST";
            }

            public Post(string url, string contenttype, int contentlength, Action<Stream> body)
                : base(url)
            {
                RequestBodyDelegate = body;
                RequestContentLength = contentlength;
                RequestContentType = contenttype;
                Method = "POST";
            }

            public Post(string url, string contenttype, Action<Stream> body)
                : base(url)
            {
                UseChunkedEncoding = true;
                RequestBodyDelegate = body;
                RequestContentType = contenttype;
                Method = "POST";
            }
        }

        public class Get : Request
        {
            public Get(string url) : base(url)
            {
            }
        }

        public class Head : Request
        {
            public Head(string url)
                : base(url)
            {
                Method = "HEAD";
            }
        }

        public class Delete : Request
        {
            public Delete(string url)
                : base(url)
            {
                Method = "DELETE";
            }
        }

        public class Put : Post
        {
            public Put(string url) : base(url)
            {
                Method = "PUT";
            }

            public Put(string url, IDictionary<string, string> postValues) : base(url, postValues)
            {
                Method = "PUT";
            }

            public Put(string url, string contenttype, string body) : base(url, contenttype, body)
            {
                Method = "PUT";
            }

            public Put(string url, string contenttype, byte[] body) : base(url, contenttype, body)
            {
                Method = "PUT";
            }

            public Put(string url, string contenttype, int contentlength, Action<Stream> body) : base(url, contenttype, contentlength, body)
            {
                Method = "PUT";
            }
        }

        public class Copy : Request
        {
            public Copy(string url) : base(url)
            {
                Method = "COPY";
            }
        }

        public class Patch : Post
        {
            public Patch(string url) : base(url)
            {
                Method = "PATCH";
            }

            public Patch(string url, IDictionary<string, string> postValues) : base(url, postValues)
            {
                Method = "PATCH";
            }

            public Patch(string url, string contenttype, string body) : base(url, contenttype, body)
            {
                Method = "PATCH";
            }

            public Patch(string url, string contenttype, byte[] body) : base(url, contenttype, body)
            {
                Method = "PATCH";
            }

            public Patch(string url, string contenttype, int contentlength, Action<Stream> body) : base(url, contenttype, contentlength, body)
            {
                Method = "PATCH";
            }
        }

        public class Move : Request
        {
            public Move(string url) : base(url)
            {
                Method = "MOVE";
            }
        }
    }
}
