/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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

        public class InvalidJSON20RPCResponseException : Exception
        {
            public InvalidJSON20RPCResponseException()
            {

            }
        }

        public class JSON20RPCException : Exception
        {
            public JSON20RPCException()
            {

            }
        }

        public static Map DeserializeResponse(Stream stream)
        {
            IValue iv = JSON.Deserialize(stream);
            if(!(iv is Map))
            {
                throw new InvalidJSON20RPCResponseException();
            }
            Map m = (Map)iv;
            if(!m.ContainsKey("_Result"))
            {
                throw new InvalidJSON20RPCResponseException();
            }
            Map res = (Map)m["_Result"];
            if(res.ContainsKey("error"))
            {
                throw new JSON20RPCException();
            }
            return res;
        }
    }
}
