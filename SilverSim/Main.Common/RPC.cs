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

using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using SilverSim.Types.StructuredData.XMLRPC;
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

        public static XMLRPC.XmlRpcResponse DoXmlRpcRequest(string url, XMLRPC.XmlRpcRequest req, int timeoutms)
        {
            return XMLRPC.DeserializeResponse(HttpClient.HttpRequestHandler.DoStreamRequest("POST", url, null, "text/xml", UTF8NoBOM.GetString(req.Serialize()), false, timeoutms));
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
