using Nwc.XmlRpc;
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

        public class XmlRpcFaultException : Exception
        {
            public XmlRpcFaultException()
            {

            }
        }

        public static XmlRpcResponse DoXmlRpcRequest(string url, XmlRpcRequest req, int timeoutms)
        {
            XmlRpcSerializer serializer = new XmlRpcSerializer();
            XmlRpcDeserializer deserializer = new XmlRpcDeserializer();
            XmlRpcResponse res = (XmlRpcResponse)deserializer.Deserialize(HttpClient.HttpRequestHandler.DoRequest("POST", url, null, "text/xml", serializer.Serialize(req), false, timeoutms));
            if(res.IsFault)
            {
                throw new XmlRpcFaultException();
            }
            return res;
        }
    }
}
