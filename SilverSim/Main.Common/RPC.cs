// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.StructuredData.Json;
using SilverSim.Types;
using SilverSim.Http.Client;
using SilverSim.Types.StructuredData.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Main.Common.Rpc
{
    public static class RPC
    {
        public static IValue DoJson20RpcRequest(string url, string method, string jsonId, IValue param, int timeoutms)
        {
            string jsonReq = Json20Rpc.SerializeRequest(method, jsonId, param);
            return Json20Rpc.DeserializeResponse(HttpRequestHandler.DoStreamRequest("POST", url, null, "application/json-rpc", jsonReq, false, timeoutms));
        }

        public static XmlRpc.XmlRpcResponse DoXmlRpcRequest(string url, XmlRpc.XmlRpcRequest req, int timeoutms)
        {
            return XmlRpc.DeserializeResponse(HttpRequestHandler.DoStreamRequest("POST", url, null, "text/xml", UTF8NoBOM.GetString(req.Serialize()), false, timeoutms));
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
