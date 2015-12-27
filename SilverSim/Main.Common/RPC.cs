// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.XmlRpc;
using System.IO;

namespace SilverSim.Main.Common.Rpc
{
    public static class RPC
    {
        public static IValue DoJson20RpcRequest(string url, string method, string jsonId, IValue param, int timeoutms)
        {
            string jsonReq = Json20Rpc.SerializeRequest(method, jsonId, param);
            using (Stream res = HttpRequestHandler.DoStreamRequest("POST", url, null, "application/json-rpc", jsonReq, false, timeoutms))
            {
                return Json20Rpc.DeserializeResponse(res);
            }
        }

        public static XmlRpc.XmlRpcResponse DoXmlRpcRequest(string url, XmlRpc.XmlRpcRequest req, int timeoutms)
        {
            using (Stream res = HttpRequestHandler.DoStreamRequest("POST", url, null, "text/xml", req.Serialize().FromUTF8Bytes(), false, timeoutms))
            {
                return XmlRpc.DeserializeResponse(res);
            }
        }
    }
}
