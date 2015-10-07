// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using SilverSim.Http.Client;
using SilverSim.Types.StructuredData.XMLRPC;
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
            string jsonReq = JSON20RPC.SerializeRequest(method, jsonId, param);
            return JSON20RPC.DeserializeResponse(HttpRequestHandler.DoStreamRequest("POST", url, null, "application/json-rpc", jsonReq, false, timeoutms));
        }

        public static XMLRPC.XmlRpcResponse DoXmlRpcRequest(string url, XMLRPC.XmlRpcRequest req, int timeoutms)
        {
            return XMLRPC.DeserializeResponse(HttpRequestHandler.DoStreamRequest("POST", url, null, "text/xml", UTF8NoBOM.GetString(req.Serialize()), false, timeoutms));
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
