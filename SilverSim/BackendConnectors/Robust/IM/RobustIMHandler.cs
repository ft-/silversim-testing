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

using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using Nini.Config;
using Nwc.XmlRpc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.BackendConnectors.Robust.IM
{
    #region Service Implementation
    class RobustIMHandler : IPlugin
    {
        bool m_DisallowOfflineIM;
        IMServiceInterface m_IMService;
        public RobustIMHandler(bool disallowOfflineIM)
        {
            m_DisallowOfflineIM = disallowOfflineIM;
        }

        public void Startup(ConfigurationLoader loader)
        {
            HttpXmlRpcHandler xmlRpc = loader.GetService<HttpXmlRpcHandler>("XmlRpcServer");
            xmlRpc.XmlRpcMethods.Add("grid_instant_message", IMReceived);
            m_IMService = loader.GetService<IMServiceInterface>("IMService");
        }

        public XmlRpcResponse IMReceived(XmlRpcRequest req)
        {
            GridInstantMessage im = new GridInstantMessage();
            XmlRpcResponse res = new XmlRpcResponse();
            try
            {
                im.NoOfflineIMStore = m_DisallowOfflineIM;
                IDictionary d = (IDictionary)req.Params[0];

                im.FromAgent.ID = (string)d["from_agent_id"];
                im.ToAgent.ID = (string)d["to_agent_id"];
                im.IMSessionID = (string)d["im_session_id"];
                im.RegionID = (string)d["region_id"];
                im.Timestamp = Date.UnixTimeToDateTime(ulong.Parse((string)d["timestamp"]));
                im.FromAgent.FullName = (string)d["from_agent_name"];
                if(d.Contains("message"))
                {
                    im.Message = (string)d["message"];
                }
                byte[] dialog = Convert.FromBase64String(d["dialog"].ToString());
                im.Dialog = (GridInstantMessageDialog)dialog[0];
                im.IsFromGroup = bool.Parse(d["from_group"].ToString());
                byte[] offline = Convert.FromBase64String(d["offline"].ToString());
                im.IsOffline = offline[0] != 0;
                im.ParentEstateID = (int)d["parent_estate_id"];
                im.Position.X = float.Parse(d["position_x"].ToString());
                im.Position.Y = float.Parse(d["position_y"].ToString());
                im.Position.Z = float.Parse(d["position_z"].ToString());
                if(d.Contains("binary_bucket"))
                {
                    im.BinaryBucket = Convert.FromBase64String(d["binary_bucket"].ToString());
                }
            }
            catch
            {
                res.SetFault(-32602, "invalid method parameters");
                return res;
            }

            ManualResetEvent e = new ManualResetEvent(false);
            Dictionary<string, string> p;
            im.OnResult = delegate(GridInstantMessage _im, bool result) { _im.ResultInfo = result;  e.Set(); };
            m_IMService.Send(im);
            try
            {
                e.WaitOne(15000);
            }
            catch
            {
                p = new Dictionary<string,string>();
                p["result"] = "FALSE";
                res.Value = p;
                return res;
            }

            p = new Dictionary<string, string>();
            p["result"] = im.ResultInfo ? "TRUE" : "FALSE";
            res.Value = p;

            return res;
        }
    }
    #endregion

    #region Factory
    public class RobustIMHandlerFactory : IPluginFactory
    {
        public RobustIMHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new RobustIMHandler(ownSection.GetBoolean("DisallowOfflineIM", true));
        }
    }
    #endregion
}
