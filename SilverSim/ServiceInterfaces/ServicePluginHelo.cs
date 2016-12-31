// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace SilverSim.ServiceInterfaces
{
    public abstract class ServicePluginHelo
    {
        public ServicePluginHelo()
        {

        }

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

            Dictionary<string, string> headers = new Dictionary<string, string>();
            try
            {
                using (Stream responseStream = HttpClient.DoStreamRequest("HEAD", uri, null, string.Empty, string.Empty, false, 20000, headers))
                {
                    using (StreamReader reader = new StreamReader(responseStream))
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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public static string[] HeloRequest_HandleType(string uri)
        {
            Dictionary<string, string> headers = HeloRequest(uri);
            string protocols;
            if(!headers.TryGetValue("X-Protocols-Provided", out protocols) &&
                !headers.TryGetValue("X-Handlers-Provided",out protocols))
            {
                protocols = "opensim-robust";
            }
            return protocols.Split(',');
        }

        public abstract string Name { get; }

        public bool IsProtocolSupported(string url)
        {
            return HeloRequest_HandleType(url).Contains(Name);
        }
    }
}
