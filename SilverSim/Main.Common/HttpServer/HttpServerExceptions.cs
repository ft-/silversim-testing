// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    [Serializable]
    public class HttpServerException : Exception
    {
        public HttpStatusCode HttpStatus { get; set; }
        public HttpServerException(HttpStatusCode httpstatus, string message)
            : base(message)
        {
            HttpStatus = httpstatus;
        }

        public virtual void Serialize(HttpRequest req)
        {
            using (req.BeginResponse(HttpStatus, Message))
            {
                /* intentionally left empty */
            }
        }
    }

    [Serializable]
    public class HttpRedirectKeepVerbException : HttpServerException
    {
        string Location { get; set; }
        public HttpRedirectKeepVerbException(string url)
            : base(HttpStatusCode.RedirectKeepVerb, "Moved permanently")
        {
            Location = url;
        }
        public override void Serialize(HttpRequest req)
        {
            using (HttpResponse res = req.BeginResponse(HttpStatus, Message))
            {
                /* intentionally left empty */
                res.Headers.Add("Location", Location);
            }
        }
    }

    [Serializable]
    public class NotAWebSocketRequestException : Exception
    {
        public NotAWebSocketRequestException()
        {
        }
    }

}
