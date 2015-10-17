// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.StructuredData.JSON
{
    public static class JSON20RPC
    {
        public static string SerializeRequest(string method, string jsonId, IValue param)
        {
            Map request = new Map();
            request.Add("jsonrpc", "2.0");
            request.Add("id", jsonId);
            request.Add("method", method);
            request.Add("params", param);
            return JSON.Serialize(request);
        }

        [Serializable]
        public class InvalidJSON20RPCResponseException : Exception
        {
            public InvalidJSON20RPCResponseException()
            {

            }
        }

        [Serializable]
        public class JSON20RPCException : Exception
        {
            public int FaultCode;
            public JSON20RPCException(int faultCode, string message)
                : base(message)
            {
                FaultCode = faultCode;
            }
        }

        public static IValue DeserializeResponse(Stream stream)
        {
            IValue iv = JSON.Deserialize(stream);
            if(!(iv is Map))
            {
                throw new InvalidJSON20RPCResponseException();
            }
            Map m = (Map)iv;
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
                throw new JSON20RPCException(faultCode, faultString);
            }
            if (!m.ContainsKey("result"))
            {
                throw new InvalidJSON20RPCResponseException();
            }
            return m["result"];
        }
    }
}
