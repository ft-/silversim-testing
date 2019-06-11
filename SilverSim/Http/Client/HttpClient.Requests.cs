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
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        public static class Rfc8187
        {
            public static string Encode(string val)
            {
                byte[] str = val.ToUTF8Bytes();
                var sb = new StringBuilder();
                bool isUtf8 = false;
                foreach(byte b in str)
                {
                    char c = (char)b;
                    if(b > 0x20 && b <= 0x7F && c != '?' && c != '=')
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        isUtf8 = true;
                        sb.AppendFormat("%{0:X2}", b);
                    }
                }

                return isUtf8 ? ("utf-8''" + sb.ToString()) : sb.ToString();
            }

            public static string Decode(string encoded)
            {
                int n = encoded.Length;
                int i;
                if(!encoded.StartsWith("utf-8'", true, CultureInfo.InvariantCulture))
                {
                    return encoded;
                }

                i = encoded.IndexOf("'", 6);
                if(i < 0)
                {
                    throw new InvalidDataException();
                }

                ++i;
                StringBuilder sb = new StringBuilder();

                while (i < n)
                {
                    char c = encoded[i++];
                    if(c == '%')
                    {
                        if(i + 2 > n)
                        {
                            break;
                        }
                        sb.Append(Convert.ToByte(encoded.Substring(i, 2), 16));
                        i += 2;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }
        }

        public interface IRequestBodyType
        {
            string ContentType { get; }
            void WriteRequestBody(Stream s);
        }

        public sealed class JsonRequest : IRequestBodyType
        {
            public string ContentType => "application/json";
            private readonly IValue m_Data;

            public void WriteRequestBody(Stream s)
            {
                Json.Serialize(m_Data, s);
            }

            public JsonRequest(IValue iv)
            {
                m_Data = iv;
            }
        }

        public sealed class LlsdXmlRequest : IRequestBodyType
        {
            public string ContentType => "application/llsd+xml";
            private readonly IValue m_Data;

            public void WriteRequestBody(Stream s)
            {
                LlsdXml.Serialize(m_Data, s);
            }

            public LlsdXmlRequest(IValue iv)
            {
                m_Data = iv;
            }
        }

        public class Request
        {
            protected virtual string DefaultMethod => "GET";

            public string Method;
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
            public bool EnableIPv6;

            public X509CertificateCollection ClientCertificates;
            public SslProtocols EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            public bool CheckCertificateRevocation = true;
            public IHttpAuthorization Authorization;
            public RemoteCertificateValidationCallback RemoteCertificateValidationCallback;
            public HttpStatusCode StatusCode;
            [Flags]
            public enum DisableExceptionFlags
            {
                None = 0,
                Disable3XX = 0x00000001,
                DisableUnauthorized = 0x00000002,
                DisableNotFound = 0x00000004,
                DisableConflict = 0x00000008,
                DisableGone = 0x00000010,
                Disable5XX = 0x00000020,
                DisableForbidden = 0x00000040,
                DisableNotModified = 0x00000080
            }

            public DisableExceptionFlags DisableExceptions;

            public bool IsExceptionDisabled()
            {
                switch(StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return (DisableExceptions & DisableExceptionFlags.DisableUnauthorized) != 0;
                    case HttpStatusCode.NotFound:
                        return (DisableExceptions & DisableExceptionFlags.DisableNotFound) != 0;
                    case HttpStatusCode.Gone:
                        return (DisableExceptions & DisableExceptionFlags.DisableGone) != 0;
                    case HttpStatusCode.Conflict:
                        return (DisableExceptions & DisableExceptionFlags.DisableConflict) != 0;
                    case HttpStatusCode.Forbidden:
                        return (DisableExceptions & DisableExceptionFlags.DisableForbidden) != 0;
                    case HttpStatusCode.NotModified:
                        return (DisableExceptions & (DisableExceptionFlags.Disable3XX | DisableExceptionFlags.DisableNotModified)) != 0;
                    default:
                        int statusCodeRange = ((int)StatusCode) / 100;
                        switch(statusCodeRange)
                        {
                            case 3:
                                return (DisableExceptions & DisableExceptionFlags.Disable3XX) != 0;

                            case 5:
                                return (DisableExceptions & DisableExceptionFlags.Disable5XX) != 0;

                            default:
                                break;
                        }
                        break;
                }
                return false;
            }

            public Request(string url)
            {
                Method = DefaultMethod;
                Url = url;
            }
        }

        public class Post : Request
        {
            protected override string DefaultMethod => "POST";

            public Post(string url)
                : base(url)
            {
                Method = DefaultMethod;
            }

            public Post(string url, IDictionary<string, string> postValues)
                : base(url)
            {
                RequestBody = BuildQueryString(postValues).ToUTF8Bytes();
                RequestContentType = "application/x-www-form-urlencoded";
                Method = DefaultMethod;
            }

            public Post(string url, IRequestBodyType typeddata)
                : base(url)
            {
                UseChunkedEncoding = true;
                RequestBodyDelegate = typeddata.WriteRequestBody;
                RequestContentType = typeddata.ContentType;
                Method = DefaultMethod;
            }

            public Post(string url, string contenttype, string body)
                : base(url)
            {
                RequestBody = body.ToUTF8Bytes();
                RequestContentType = contenttype;
                Method = DefaultMethod;
            }

            public Post(string url, string contenttype, byte[] body)
                : base(url)
            {
                RequestBody = body;
                RequestContentType = contenttype;
                Method = DefaultMethod;
            }

            public Post(string url, string contenttype, int contentlength, Action<Stream> body)
                : base(url)
            {
                RequestBodyDelegate = body;
                RequestContentLength = contentlength;
                RequestContentType = contenttype;
                Method = DefaultMethod;
            }

            public Post(string url, string contenttype, Action<Stream> body)
                : base(url)
            {
                UseChunkedEncoding = true;
                RequestBodyDelegate = body;
                RequestContentType = contenttype;
                Method = DefaultMethod;
            }

            public Post(string url, string contenttype, Action<XmlTextWriter> body)
                : base(url)
            {
                UseChunkedEncoding = true;
                RequestBodyDelegate = (s) =>
                {
                    using (XmlTextWriter writer = s.UTF8XmlTextWriter())
                    {
                        body(writer);
                    }
                };
                RequestContentType = contenttype;
                Method = DefaultMethod;
            }

            public Post(string url, Action<XmlTextWriter> body)
                : base(url)
            {
                UseChunkedEncoding = true;
                RequestBodyDelegate = (s) =>
                {
                    using (XmlTextWriter writer = s.UTF8XmlTextWriter())
                    {
                        body(writer);
                    }
                };
                RequestContentType = "application/xml";
                Method = DefaultMethod;
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
            protected override string DefaultMethod => "HEAD";

            public Head(string url) : base(url)
            {
            }
        }

        public class Delete : Request
        {
            protected override string DefaultMethod => "DELETE";

            public Delete(string url) : base(url)
            {
            }
        }

        public sealed class Put : Post
        {
            protected override string DefaultMethod => "PUT";

            public Put(string url) : base(url)
            {
            }

            public Put(string url, IDictionary<string, string> postValues) : base(url, postValues)
            {
            }

            public Put(string url, IRequestBodyType typeddata) : base(url, typeddata)
            {
            }

            public Put(string url, Action<XmlTextWriter> body) : base(url, body)
            {
            }

            public Put(string url, string contenttype, string body) : base(url, contenttype, body)
            {
            }

            public Put(string url, string contenttype, byte[] body) : base(url, contenttype, body)
            {
            }

            public Put(string url, string contenttype, Action<Stream> body) : base(url, contenttype, body)
            {
            }

            public Put(string url, string contenttype, Action<XmlTextWriter> body) : base(url, contenttype, body)
            {
            }

            public Put(string url, string contenttype, int contentlength, Action<Stream> body) : base(url, contenttype, contentlength, body)
            {
            }
        }

        public class Patch : Post
        {
            protected override string DefaultMethod => "PATCH";

            public Patch(string url) : base(url)
            {
            }

            public Patch(string url, IDictionary<string, string> postValues) : base(url, postValues)
            {
            }

            public Patch(string url, IRequestBodyType typeddata) : base(url, typeddata)
            {
            }

            public Patch(string url, Action<XmlTextWriter> body) : base(url, body)
            {
            }

            public Patch(string url, string contenttype, string body) : base(url, contenttype, body)
            {
            }

            public Patch(string url, string contenttype, byte[] body) : base(url, contenttype, body)
            {
            }

            public Patch(string url, string contenttype, Action<Stream> body) : base(url, contenttype, body)
            {
            }

            public Patch(string url, string contenttype, Action<XmlTextWriter> body) : base(url, contenttype, body)
            {
            }

            public Patch(string url, string contenttype, int contentlength, Action<Stream> body) : base(url, contenttype, contentlength, body)
            {
            }
        }

        public class Copy : Request
        {
            protected override string DefaultMethod => "COPY";

            public Copy(string url) : base(url)
            {
            }

            public Copy(string url, string desturl) : base(url)
            {
                Headers = new Dictionary<string, string>
                {
                    ["Destination"] = desturl
                };
            }
        }

        public class Move : Request
        {
            protected override string DefaultMethod => "MOVE";

            public Move(string url) : base(url)
            {
            }

            public Move(string url, string desturl) : base(url)
            {
                Headers = new Dictionary<string, string>
                {
                    ["Destination"] = desturl
                };
            }
        }
    }
}
