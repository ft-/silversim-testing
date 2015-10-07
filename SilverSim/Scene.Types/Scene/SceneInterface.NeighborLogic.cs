// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Grid;
using SilverSim.Http.Client;
using System.Collections.Generic;
using System.IO;
using SilverSim.Types;
using SilverSim.StructuredData.LLSD;

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
            if(rinfo.ServerURI == RegionData.ServerURI)
            {
                /* ignore same instance */
                return;
            }

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

        void EnableSimCircuit(RegionInfo destinationInfo, out UUID sessionID, out uint circuitCode)
        {
            Map reqmap = new Map();
            reqmap["to_region_id"] = destinationInfo.ID;
            reqmap["from_region_id"] = ID;
            reqmap["scope_id"] = RegionData.ScopeID;
            byte[] reqdata;
            using(MemoryStream ms = new MemoryStream())
            {
                LLSD_XML.Serialize(reqmap, ms);
                reqdata = ms.GetBuffer();
            }

            IValue iv = LLSD_XML.Deserialize(
                HttpRequestHandler.DoStreamRequest(
                "POST", 
                destinationInfo.ServerURI + "circuit",
                null,
                "application/llsd+xml",
                reqdata.Length,
                delegate(Stream s)
                {
                    s.Write(reqdata, 0, reqdata.Length);
                },
                false,
                10000,
                null));

            Map resmap = (Map)iv;
            circuitCode = resmap["circuit_code"].AsUInt;
            sessionID = resmap["session_id"].AsUUID;
        }
    }
}
