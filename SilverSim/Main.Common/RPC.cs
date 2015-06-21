using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Main.Common.Rpc
{
    public static class RPC
    {
        public static Map DoJson20RpcRequest(string url, string method, string jsonId, IValue param, int timeoutms)
        {
            string jsonReq = JSON20RPC.SerializeRequest(method, jsonId, param);
            return JSON20RPC.DeserializeResponse(HttpClient.HttpRequestHandler.DoStreamRequest("POST", url, null, "application/json-rpc", jsonReq, false, timeoutms));
        }
    }
}
