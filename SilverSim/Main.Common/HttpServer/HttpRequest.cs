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
using SilverSim.Types;
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace SilverSim.Main.Common.HttpServer
{
    public enum HttpConnectionMode
    {
        Close,
        KeepAlive
    }

    public abstract class HttpRequest
    {
        #region Private Fields
        internal readonly Dictionary<string, string> m_Headers = new Dictionary<string, string>();
        #endregion

        #region Properties
        public uint MajorVersion { get; protected set; }
        public uint MinorVersion { get; protected set; }
        public string RawUrl { get; protected set; }
        public string Method { get; protected set; }
        public abstract Stream Body { get; }
        public abstract bool HasRequestBody { get; }
        public HttpConnectionMode ConnectionMode { get; protected set; }
        public HttpResponse Response { get; protected set; }
        public string CallerIP { get; protected set; }
        public bool Expect100Continue { get; protected set; }
        public bool IsH2CUpgradable { get; protected set; }
        public bool IsH2CUpgradableAfterReadingBody { get; protected set; }
        public bool IsSsl { get; }
        public X509Certificate RemoteCertificate { get; }

        static protected readonly Dictionary<HttpStatusCode, string> m_StatusCodeMap = new Dictionary<HttpStatusCode, string>();
        static HttpRequest()
        {
            m_StatusCodeMap.Add(HttpStatusCode.BadGateway, "Bad gateway");
            m_StatusCodeMap.Add(HttpStatusCode.BadRequest, "Bad request");
            m_StatusCodeMap.Add(HttpStatusCode.ExpectationFailed, "Expectation failed");
            m_StatusCodeMap.Add(HttpStatusCode.GatewayTimeout, "Gateway timeout");
            m_StatusCodeMap.Add(HttpStatusCode.HttpVersionNotSupported, "Http version not supported");
            m_StatusCodeMap.Add(HttpStatusCode.InternalServerError, "Internal server error");
            m_StatusCodeMap.Add(HttpStatusCode.LengthRequired, "Length required");
            m_StatusCodeMap.Add(HttpStatusCode.MethodNotAllowed, "Method not allowed");
            m_StatusCodeMap.Add(HttpStatusCode.MovedPermanently, "Moved permanently");
            m_StatusCodeMap.Add(HttpStatusCode.MultipleChoices, "Multiple choices");
            m_StatusCodeMap.Add(HttpStatusCode.NoContent, "No content");
            m_StatusCodeMap.Add(HttpStatusCode.NonAuthoritativeInformation, "Non authoritative information");
            m_StatusCodeMap.Add(HttpStatusCode.NotAcceptable, "Not acceptable");
            m_StatusCodeMap.Add(HttpStatusCode.NotFound, "Not found");
            m_StatusCodeMap.Add(HttpStatusCode.NotImplemented, "Not implemented");
            m_StatusCodeMap.Add(HttpStatusCode.NotModified, "Not modified");
            m_StatusCodeMap.Add(HttpStatusCode.PartialContent, "Partial content");
            m_StatusCodeMap.Add(HttpStatusCode.PaymentRequired, "Payment required");
            m_StatusCodeMap.Add(HttpStatusCode.PreconditionFailed, "Precondition failed");
            m_StatusCodeMap.Add(HttpStatusCode.ProxyAuthenticationRequired, "Proxy authentication required");
            m_StatusCodeMap.Add(HttpStatusCode.RedirectMethod, "Redirect method");
            m_StatusCodeMap.Add(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
            m_StatusCodeMap.Add(HttpStatusCode.RequestEntityTooLarge, "Request entity too large");
            m_StatusCodeMap.Add(HttpStatusCode.RequestTimeout, "Request timeout");
            m_StatusCodeMap.Add(HttpStatusCode.RequestUriTooLong, "Request uri too long");
            m_StatusCodeMap.Add(HttpStatusCode.ResetContent, "Reset content");
            m_StatusCodeMap.Add(HttpStatusCode.ServiceUnavailable, "Service unavailable");
            m_StatusCodeMap.Add(HttpStatusCode.SwitchingProtocols, "Switching protocols");
            m_StatusCodeMap.Add(HttpStatusCode.TemporaryRedirect, "Temporary redirect");
            m_StatusCodeMap.Add(HttpStatusCode.UnsupportedMediaType, "Unsupported media type");
            m_StatusCodeMap.Add(HttpStatusCode.UpgradeRequired, "Upgrade required");
            m_StatusCodeMap.Add(HttpStatusCode.UseProxy, "Use proxy");
        }

        public bool IsChunkedAccepted => m_Headers.ContainsKey("te");

        public string this[string fieldName]
        {
            get { return m_Headers[fieldName.ToLowerInvariant()]; }

            set { m_Headers[fieldName.ToLowerInvariant()] = value; }
        }

        public bool TryGetHeader(string fieldName, out string value) => m_Headers.TryGetValue(fieldName.ToLowerInvariant(), out value);

        public bool ContainsHeader(string fieldName) => m_Headers.ContainsKey(fieldName.ToLowerInvariant());

        public string ContentType
        {
            get
            {
                if (m_Headers.ContainsKey("content-type"))
                {
                    string contentType = m_Headers["content-type"];
                    int semi = contentType.IndexOf(';');
                    return semi >= 0 ? contentType.Substring(0, semi).Trim() : contentType;
                }
                return string.Empty;
            }

            set { m_Headers["content-type"] = value; }
        }
        #endregion

        public string GetUrlAndQueryParams(List<KeyValuePair<string, string>> query)
        {
            if(query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            int qpos = RawUrl.IndexOf('?');
            query.Clear();
            if (qpos < 0)
            {
                return RawUrl;
            }
            else
            {
                string url = RawUrl.Substring(0, qpos);
                foreach(string qp in RawUrl.Substring(qpos + 1).Split('&'))
                {
                    string qptrimmed = qp.Trim();
                    if(qptrimmed.Length == 0)
                    {
                        continue;
                    }
                    string[] vp = qptrimmed.Split('=');
                    if (vp.Length < 2)
                    {
                        query.Add(new KeyValuePair<string, string>(vp[0], string.Empty));
                    }
                    else
                    {
                        query.Add(new KeyValuePair<string, string>(vp[0], vp[1]));
                    }
                }
                return url;
            }
        }

        public string GetUrlAndQueryParams(Dictionary<string, string> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            int qpos = RawUrl.IndexOf('?');
            query.Clear();
            if (qpos < 0)
            {
                return RawUrl;
            }
            else
            {
                string url = RawUrl.Substring(0, qpos);
                foreach (string qp in RawUrl.Substring(qpos + 1).Split('&'))
                {
                    string qptrimmed = qp.Trim();
                    if (qptrimmed.Length == 0)
                    {
                        continue;
                    }
                    string[] vp = qptrimmed.Split('=');
                    query[vp[0]] = vp.Length < 2 ? string.Empty : vp[1];
                }
                return url;
            }
        }

        public abstract void Close();

        public abstract void SetConnectionClose();

        protected HttpRequest(bool isSsl, X509Certificate remoteCertificate)
        {
            IsSsl = isSsl;
            RemoteCertificate = remoteCertificate;
        }

        public abstract HttpResponse BeginResponse();

        public void EmptyResponse(string contentType = "text/plain")
        {
            using (HttpResponse res = BeginResponse(contentType))
            using (res.GetOutputStream(0))
            {
                /* intentionally left empty */
            }
        }

        public HttpResponse BeginResponse(string contentType)
        {
            HttpResponse res = BeginResponse();
            res.ContentType = contentType;
            return res;
        }

        public HttpResponse BeginResponse(HttpStatusCode statuscode, string statusDescription, string contentType)
        {
            HttpResponse res = BeginResponse(statuscode, statusDescription);
            res.ContentType = contentType;
            return res;
        }

        public void ErrorResponse(HttpStatusCode statuscode, string statusDescription)
        {
            using(var res = BeginResponse(statuscode, statusDescription))
            {
                res.ContentType = "text/plain";
                using (res.GetOutputStream(0))
                {
                    /* intentionally left empty */
                }
            }
        }

        public void ErrorResponse(HttpStatusCode statuscode)
        {
            string msg;
            if(!m_StatusCodeMap.TryGetValue(statuscode, out msg))
            {
                msg = statuscode.ToString();
            }
            ErrorResponse(statuscode, msg);
        }

        public abstract HttpResponse BeginResponse(HttpStatusCode statuscode, string statusDescription);

        public abstract HttpResponse BeginChunkedResponse();

        public abstract HttpResponse BeginChunkedResponse(string contentType);

        public bool IsWebSocket
        {
            get
            {
                if(Method != "GET")
                {
                    return false;
                }
                if(!(MajorVersion > 1 || (MajorVersion == 1 && MinorVersion >= 1)))
                {
                    return false;
                }

                string val;
                if(!m_Headers.TryGetValue("upgrade", out val) || val.ToLower() != "websocket")
                {
                    return false;
                }

                if(!m_Headers.ContainsKey("sec-websocket-key"))
                {
                    return false;
                }
                if(!m_Headers.TryGetValue("sec-websocket-version", out val) || val != "13")
                {
                    return false;
                }

                /* Connection header is checked last */
                if (!m_Headers.TryGetValue("connection", out val))
                {
                    return false;
                }

                foreach (string valitem in val.Split(','))
                {
                    if (valitem.ToLowerInvariant().Trim() == "upgrade")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public abstract HttpWebSocket BeginWebSocket(string websocketprotocol = "");

        public void JsonResponse(IValue iv)
        {
            if (Method == "HEAD")
            {
                int cLength;
                using (var ms = new MemoryStream())
                {
                    Json.Serialize(iv, ms);
                    cLength = ms.ToArray().Length;
                }
                using (HttpResponse res = BeginResponse("application/json"))
                {
                    res.Headers["Content-Length"] = cLength.ToString();
                }
            }
            else
            {
                using (HttpResponse res = BeginResponse("application/json"))
                {
                    using (Stream s = res.GetOutputStream())
                    {
                        Json.Serialize(iv, s);
                    }
                }
            }
        }

        public void LlsdXmlResponse(IValue iv)
        {
            if (Method == "HEAD")
            {
                int cLength;
                using (var ms = new MemoryStream())
                {
                    LlsdXml.Serialize(iv, ms);
                    cLength = ms.ToArray().Length;
                }
                using (HttpResponse res = BeginResponse("application/json"))
                {
                    res.Headers["Content-Length"] = cLength.ToString();
                }
            }
            else
            {
                using (HttpResponse res = BeginResponse("application/llsd+xml"))
                {
                    using (Stream s = res.GetOutputStream())
                    {
                        LlsdXml.Serialize(iv, s);
                    }
                }
            }
        }

        public void LlsdBinaryResponse(IValue iv, bool writeHeader = false)
        {
            if (Method == "HEAD")
            {
                int cLength;
                using (var ms = new MemoryStream())
                {
                    LlsdBinary.Serialize(iv, ms);
                    cLength = ms.ToArray().Length;
                }
                using (HttpResponse res = BeginResponse("application/json"))
                {
                    res.Headers["Content-Length"] = cLength.ToString();
                }
            }
            else
            {
                using (HttpResponse res = BeginResponse("application/llsd+binary"))
                {
                    using (Stream s = res.GetOutputStream())
                    {
                        LlsdBinary.Serialize(iv, s, writeHeader);
                    }
                }
            }
        }

        public void NotFound() => ErrorResponse(HttpStatusCode.NotFound);

        public void BadRequest() => ErrorResponse(HttpStatusCode.BadRequest);

        public void MethodNotAllowed() => ErrorResponse(HttpStatusCode.MethodNotAllowed);
    }
}
