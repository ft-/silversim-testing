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
using System.Net;
using System.Text;

namespace SilverSim.Main.Common.HttpServer.Authorization
{
    public class BasicAuthHandler
    {
        private readonly string m_Realm;
        private readonly Action<HttpRequest> m_RequestHandler;
        protected Func<string, string, bool> m_CheckPasswordHandler;

        public BasicAuthHandler(string realm, Func<string, string, bool> checkPassword, Action<HttpRequest> requestHandler)
        {
            m_Realm = realm;
            m_RequestHandler = requestHandler;
            m_CheckPasswordHandler = checkPassword;
        }

        public void HandleRequest(HttpRequest req)
        {
            string authorize;
            if(!req.TryGetHeader("authorization", out authorize))
            {
                using (HttpResponse res = req.BeginResponse(HttpStatusCode.Unauthorized, "Unauthorized"))
                {
                    res.Headers["WWW-Authenticate"] = $"Basic realm=\"{QuoteString(m_Realm)}\"";
                }
                return;
            }
            string[] auth = authorize.Trim().Split(' ');
            if(auth.Length < 2)
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            string[] basic = Convert.FromBase64String(auth[1]).FromUTF8Bytes().Split(':');
            if(basic.Length != 2)
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                return;
            }

            if(auth[0] != "Basic" || !m_CheckPasswordHandler(basic[0], basic[1]))
            {
                using (HttpResponse res = req.BeginResponse(HttpStatusCode.Unauthorized, "Unauthorized"))
                {
                    res.Headers["WWW-Authenticate"] = $"Basic realm=\"{QuoteString(m_Realm)}\"";
                }
                return;
            }

            m_RequestHandler(req);
        }

        private static string QuoteString(string input)
        {
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (c == '\\' || c == '\"')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return $"\"{sb}\"";
        }
    }
}
