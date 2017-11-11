﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Main.Common.Rpc;
using SilverSim.Types;
using SilverSim.Types.ServerURIs;
using SilverSim.Types.StructuredData.XmlRpc;
using System;

namespace SilverSim.Main.Common
{
    public static class ServerURIsExtensionMethods
    {
        public static void GetServerURLs(this ServerURIs serverUrls, UUI user, string uri = null, int timeoutms = 20000)
        {
            var hash = new Map
            {
                ["userID"] = user.ID
            };
            Map res = DoXmlRpcWithHashResponse(uri ?? user.HomeURI.ToString(), "get_server_urls", hash, timeoutms);
            serverUrls.Clear();
            foreach (string key in res.Keys)
            {
                if (key.StartsWith("SRV_") && res[key] != null)
                {
                    string serverType = key.Substring(4);
                    serverUrls.Add(serverType, res[key].ToString());
                }
            }
        }

        private static Map DoXmlRpcWithHashResponse(string uri, string method, Map reqparams, int timeoutms)
        {
            var req = new XmlRpc.XmlRpcRequest(method);
            req.Params.Add(reqparams);
            XmlRpc.XmlRpcResponse res = RPC.DoXmlRpcRequest(uri, req, timeoutms);

            var hash = (Map)res.ReturnValue;
            if (hash == null)
            {
                throw new InvalidOperationException();
            }

            return hash;
        }
    }
}
