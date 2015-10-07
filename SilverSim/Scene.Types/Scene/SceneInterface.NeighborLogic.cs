// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Grid;
using SilverSim.Http.Client;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public virtual void NotifyNeighborOnline(RegionInfo rinfo)
        {
            VerifyNeighbor(rinfo);
        }

        public virtual void NotifyNeighborOffline(RegionInfo rinfo)
        {

        }

        void VerifyNeighbor(RegionInfo rinfo)
        {
            Dictionary<string, string> headers = new Dictionary<string,string>();
            try
            {
                using (Stream responseStream = HttpRequestHandler.DoStreamRequest("HEAD", rinfo.ServerURI + "helo", null, "", "", false, 20000, headers))
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string ign = reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                headers.Clear();
            }

            if(headers.ContainsKey("X-UDP-InterSim"))
            {
                /* neighbor supports UDP Inter-Sim connects */
            }
        }
    }
}
