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

using SilverSim.Http.Client;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SilverSim.ServiceInterfaces
{
    public abstract class ServicePluginHelo
    {
        public static Dictionary<string, string> HeloRequest(string uri)
        {
            if (!uri.EndsWith("="))
            {
                uri = uri.TrimEnd('/') + "/helo/";
            }
            else
            {
                /* simian special */
                if (uri.Contains("?"))
                {
                    uri = uri.Substring(0, uri.IndexOf('?'));
                }
                uri = uri.TrimEnd('/') + "/helo/";
            }

            var headers = new Dictionary<string, string>();
            try
            {
                using (var responseStream = HttpClient.DoStreamRequest("HEAD", uri, null, string.Empty, string.Empty, false, 20000, headers))
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                headers.Clear();
            }
            return headers;
        }

        public static string[] HeloRequest_HandleType(string uri, Dictionary<string, string> cachedheaders = null)
        {
            var headers = cachedheaders ?? HeloRequest(uri);
            string protocols;
            if(!headers.TryGetValue("x-protocols-provided", out protocols) &&
                !headers.TryGetValue("x-handlers-provided",out protocols))
            {
                protocols = "opensim-robust";
            }
            return protocols.Split(',');
        }

        public abstract string Name { get; }

        public bool IsProtocolSupported(string url) => HeloRequest_HandleType(url).Contains(Name);

        public bool IsProtocolSupported(string url, Dictionary<string, string> cachedheaders) => HeloRequest_HandleType(url, cachedheaders).Contains(Name);
    }
}
