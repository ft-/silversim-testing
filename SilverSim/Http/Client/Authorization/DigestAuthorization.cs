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
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SilverSim.Http.Client.Authorization
{
    public sealed class DigestAuthorization : IHttpAuthorization
    {
        private readonly string m_Username;
        private readonly string m_Password;
        private string m_Nonce;
        private string m_Opaque;
        private string m_Realm;
        private string m_Algorithm;
        private int m_NonceCount;
        private bool m_UserHash;

        public DigestAuthorization(string username, string password)
        {
            m_Username = username;
            m_Password = password;
        }

        public void GetRequestHeaders(IDictionary<string, string> headers, string method, string requestUri)
        {
            if (!string.IsNullOrEmpty(m_Algorithm))
            {
                var nc = (uint)Interlocked.Increment(ref m_NonceCount);
                string auth = "Digest ";

                Func<string, string> hashFunction;
                if (m_Algorithm == "MD5")
                {
                    hashFunction = GetMD5;
                }
                else if (m_Algorithm == "SHA2-256")
                {
                    hashFunction = GetSHA256;
                }
                else
                {
                    m_Algorithm = string.Empty;
                    m_Nonce = string.Empty;
                    m_NonceCount = 0;
                    m_Opaque = string.Empty;
                    m_Realm = string.Empty;
                    m_UserHash = false;
                    return;
                }

                if (m_UserHash)
                {
                    string userhash = QuoteString(hashFunction($"{m_Username}:${m_Realm}"));
                    auth += $"username=\"{userhash}\", ";
                }
                else
                {
                    auth += $"username*=\"{m_Username}\", ";
                }

                var randomdata = new byte[16];
                new Random().NextBytes(randomdata);

                string clientnonce = randomdata.ToHexString();

                string ncstr = string.Format("{0:8x}", nc);
                auth += $"qop=\"auth\", realm={QuoteString(m_Realm)}, ";
                auth += $"uri={QuoteString(requestUri)}, algorithm={m_Algorithm}, nonce={QuoteString(m_Nonce)}, ";
                auth += $"nc={ncstr}, cnonce={QuoteString(clientnonce)}";

                string a1 = hashFunction($"{m_Username}:{m_Realm}:{m_Password}");
                string a2 = hashFunction($"{method}:{requestUri}");
                string response = hashFunction($"{a1}:{m_Nonce}:{ncstr}:{clientnonce}:auth:{a2}");

                auth += $", response={QuoteString(response)}";

                headers["Authorization"] = auth;
            }
        }

        private static string QuoteString(string input)
        {
            var sb = new StringBuilder();
            foreach(char c in input)
            {
                if(c == '\\' || c == '\"')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return $"\"{sb}\"";
        }

        private static string GetMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(input.ToUTF8Bytes()).ToHexString();
            }
        }

        private static string GetSHA256(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(input.ToUTF8Bytes()).ToHexString();
            }
        }

        public bool CanHandleUnauthorized(IDictionary<string, string> headers)
        {
            string auth;
            string authtype;
            if(headers.TryGetValue("www-authenticate", out auth))
            {
                foreach (string actauth in auth.Split('\0'))
                {
                    Dictionary<string, string> authpara = HttpClient.ParseWWWAuthenticate(auth, out authtype);
                    if (authpara == null || authtype != "Digest")
                    {
                        return false;
                    }

                    string val;
                    string algo;
                    if(!authpara.TryGetValue("algorithm", out algo))
                    {
                        algo = "MD5";
                    }
                    if(algo == "MD5" || algo == "SHA-256")
                    {
                        m_UserHash = authpara.TryGetValue("userhash", out val) && bool.Parse(val);
                        m_Opaque = authpara["opaque"];
                        m_Nonce = authpara["nonce"];
                        m_Realm = authpara["realm"];
                        m_Algorithm = algo;
                        return true;
                    }
                }
            }
            return false;
        }

        public void ProcessResponseHeaders(IDictionary<string, string> headers)
        {
            string authinfo;
            if (headers.TryGetValue("Authentication-Info", out authinfo))
            {
                Dictionary<string, string> authparams = HttpClient.ParseAuthParams(authinfo);
                string nextnonce;
                if (authparams != null && authparams.TryGetValue("nextnonce", out nextnonce))
                {
                    m_Nonce = nextnonce;
                }
            }
        }

        public bool IsSchemeAllowed(string scheme) => true;
    }
}
