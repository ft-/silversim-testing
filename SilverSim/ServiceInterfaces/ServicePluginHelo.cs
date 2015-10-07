// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SilverSim.ServiceInterfaces
{
    public abstract class ServicePluginHelo
    {
        public ServicePluginHelo()
        {

        }

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
                using (Stream responseStream = HttpRequestHandler.DoStreamRequest("HEAD", uri, null, "", "", false, 20000, headers))
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string ign = reader.ReadToEnd();
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
