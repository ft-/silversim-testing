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

using System;
using System.IO;
using System.Runtime.Serialization;

namespace SilverSim.Types.StructuredData.Json
{
    public static class Json20Rpc
    {
        public static string SerializeRequest(string method, string jsonId, IValue param)
        {
            var request = new Map
            {
                { "jsonrpc", "2.0" },
                { "id", jsonId },
                { "method", method },
                { "params", param }
            };
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
            var m = Json.Deserialize(stream) as Map;
            if(m == null)
            {
                throw new InvalidJson20RpcResponseException();
            }

            if (m.ContainsKey("error"))
            {
                int faultCode = -1;
                var faultString = string.Empty;
                var error = (Map)m["error"];
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
