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

using Nwc.XmlRpc;
using SilverSim.HttpClient;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.IM
{
    public class RobustIMConnector : IMServiceInterface
    {
        public int TimeoutMs { get; set; }
        private static readonly XmlRpcDeserializer m_XmlRpcDeserializer = new XmlRpcDeserializer();

        string m_IMUri;

        public RobustIMConnector(string uri)
        {
            TimeoutMs = 20000;
            m_IMUri = uri;
        }

        public override void Send(GridInstantMessage im)
        {
            Dictionary<string, object> p = new Dictionary<string, object>();
            p["from_agent_id"] = im.FromAgent.ID.ToString();
            p["from_agent_session"] = UUID.Zero.ToString();
            p["to_agent_id"] = im.ToAgent.ID.ToString();
            p["im_session_id"] = im.IMSessionID.ToString();
            p["region_id"] = im.RegionID.ToString();
            p["timestamp"] = im.Timestamp.AsULong.ToString();
            p["from_agent_name"] = im.FromAgent.FullName;
            p["from_group"] = im.IsFromGroup ? "TRUE" : "FALSE";
            p["message"] = im.Message;
            byte[] v = new byte[1];
            v[0] = (byte)(int)im.Dialog;
            p["dialog"] = Convert.ToBase64String(v, Base64FormattingOptions.None);
            v = new byte[1];
            v[0] = (im.IsOffline ? (byte)1 : (byte)0);
            p["offline"] = Convert.ToBase64String(v, Base64FormattingOptions.None);
            p["parent_estate_id"] = im.ParentEstateID.ToString();
            p["position_x"] = im.Position.X.ToString();
            p["position_y"] = im.Position.Y.ToString();
            p["position_z"] = im.Position.Z.ToString();
            p["binary_bucket"] = Convert.ToBase64String(im.BinaryBucket, Base64FormattingOptions.None); ;

            List<object> plist = new List<object>();
            plist.Add(p);
            XmlRpcRequest req = new XmlRpcRequest("grid_instant_message", plist);

            XmlRpcResponse res = (XmlRpcResponse)m_XmlRpcDeserializer.Deserialize(HttpRequestHandler.DoRequest("POST", m_IMUri, null, "text/xml", req.ToString(), false, TimeoutMs));
            if(res.IsFault)
            {
                throw new IMSendFailedException();
            }
            if(res.Value is IDictionary)
            {
                IDictionary d = (IDictionary) res.Value;
                if(bool.Parse(d["success"].ToString()))
                {
                    return;
                }
            }
            throw new IMSendFailedException();
        }
    }
}
