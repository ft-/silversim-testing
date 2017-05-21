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

using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public static class SimCircuitEstablishService
    {
        private static readonly Random m_RandomNumber = new Random();
        private static readonly object m_RandomNumberLock = new object();

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

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public static void HandleSimCircuitRequest(HttpRequest req, ConfigurationLoader loader)
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
            if(reqmap == null)
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            var regionID = reqmap["to_region_id"].AsUUID;
            var fromRegionID = reqmap["from_region_id"].AsUUID;
            var scopeID = reqmap["scope_id"].AsUUID;

            SceneInterface scene;
            try
            {
                scene = loader.Scenes[regionID];
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            var gridService = scene.GridService;
            if(gridService == null)
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

            var sessionID = UUID.Random;
            uint circuitCode = NewCircuitCode;
            Vector3 remoteOffset = regionInfo.Location - scene.GridPosition;
            var udpServer = (UDPCircuitsManager)scene.UDPServer;
            var circuit = new SimCircuit(udpServer, circuitCode, fromRegionID, sessionID, regionInfo.Location, remoteOffset);
            udpServer.AddCircuit(circuit);
            var resmap = new Map
            {
                { "circuit_code", circuitCode },
                { "session_id", sessionID }
            };
            using (var res = req.BeginResponse("application/llsd+xml"))
            {
                using (var o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resmap, o);
                }
            }
        }
    }
}
