// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public static class SimCircuitEstablishService
    {
        static Random m_RandomNumber = new Random();
        static object m_RandomNumberLock = new object();

        private static uint NewCircuitCode
        {
            get
            {
                int rand;
                lock(m_RandomNumberLock)
                {
                    rand = m_RandomNumber.Next(Int32.MinValue, Int32.MaxValue);
                }
                return (uint)rand;
            }
        }

        public static void HandleSimCircuitRequest(HttpRequest req)
        {
            if (req.ContainsHeader("X-SecondLife-Shard"))
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Request source not allowed");
                return;
            }

            if(req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(req.Body) as Map;
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }
            if(null == reqmap)
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            UUID regionID = reqmap["to_region_id"].AsUUID;
            UUID fromRegionID = reqmap["from_region_id"].AsUUID;
            UUID scopeID = reqmap["scope_id"].AsUUID;

            SceneInterface scene;
            try
            {
                scene = SceneManager.Scenes[regionID];
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            GridServiceInterface gridService = scene.GridService;
            if(null == gridService)
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            RegionInfo regionInfo;
            try
            {
                regionInfo = gridService[scopeID, fromRegionID];
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            UUID sessionID = UUID.Random;
            uint circuitCode = NewCircuitCode;
            Vector3 remoteOffset = regionInfo.Location - scene.RegionData.Location;
            UDPCircuitsManager udpServer = (UDPCircuitsManager)scene.UDPServer;
            SimCircuit circuit = new SimCircuit(udpServer, circuitCode, fromRegionID, sessionID, regionInfo.Location, remoteOffset);
            udpServer.AddCircuit(circuit);
            Map resmap = new Map();
            resmap.Add("circuit_code", circuitCode);
            resmap.Add("session_id", sessionID);
            using (HttpResponse res = req.BeginResponse("application/llsd+xml"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resmap, o);
                }
            }
        }
    }
}
