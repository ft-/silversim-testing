// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

            try
            {
                WebRequest req = HttpWebRequest.Create(uri);
                using (WebResponse response = req.GetResponse())
                {
                    if (response.Headers.Get("X-Handlers-Provided") == null)
                    {
                        return "opensim-robust"; /* let us assume Robust API */
                    }
                    return response.Headers.Get("X-Handlers-Provided");
                }
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
