// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.ServiceInterfaces
{
    public abstract class ServicePluginHelo
    {
        public ServicePluginHelo()
        {

        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        string HeloRequester(string uri)
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

            Dictionary<string, string> headers = new Dictionary<string,string>();
            try
            {
                using (Stream responseStream = HttpRequestHandler.DoStreamRequest("HEAD", uri, null, string.Empty, string.Empty, false, 20000, headers))
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        reader.ReadToEnd();
                    }
                }

                if (!headers.ContainsKey("X-Handlers-Provided"))
                {
                    return "opensim-robust"; /* let us assume Robust API */
                }
                return headers["X-Handlers-Provided"];
            }
            catch
            {
                return "opensim-robust"; /* let us assume Robust API */
            }
        }

        public abstract string Name { get; }

        public bool IsProtocolSupported(string url)
        {
            return HeloRequester(url) == Name;
        }
    }
}
