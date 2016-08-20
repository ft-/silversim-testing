// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using System.Runtime.Serialization;

namespace SilverSim.Types.StructuredData.Json
{
    public static class Json20Rpc
    {
        public static string SerializeRequest(string method, string jsonId, IValue param)
        {
            Map request = new Map();
            request.Add("jsonrpc", "2.0");
            request.Add("id", jsonId);
            request.Add("method", method);
            request.Add("params", param);
            return Json.Serialize(request);
        }

        [Serializable]
        public class InvalidJson20RpcResponseException : Exception
        {
            public InvalidJson20RpcResponseException()
            {

            }

            public InvalidJson20RpcResponseException(string message)
                : base(message)
            {

            }

            protected InvalidJson20RpcResponseException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public InvalidJson20RpcResponseException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        [Serializable]
        public class Json20RpcException : Exception
        {
            public int FaultCode;

            public Json20RpcException()
            {

            }

            public Json20RpcException(int faultCode, string message)
                : base(message)
            {
                FaultCode = faultCode;
            }

            public Json20RpcException(string message)
                : base(message)
            {

            }

            protected Json20RpcException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
                FaultCode = info.GetInt32("FaultCode");
            }

            public Json20RpcException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        public static IValue DeserializeResponse(Stream stream)
        {
            Map m = Json.Deserialize(stream) as Map;
            if(null == m)
            {
                throw new InvalidJson20RpcResponseException();
            }

            if (m.ContainsKey("error"))
            {
                int faultCode = -1;
                string faultString = string.Empty;
                Map error = (Map)m["error"];
                if(error.ContainsKey("code"))
                {
                    faultCode = error["code"].AsInt;
                }
                if(error.ContainsKey("message"))
                {
                    faultString = error["message"].ToString();
                }
                throw new Json20RpcException(faultCode, faultString);
            }
            if (!m.ContainsKey("result"))
            {
                throw new InvalidJson20RpcResponseException();
            }
            return m["result"];
        }
    }
}
