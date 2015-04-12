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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using System.Net;
using System.Text;

namespace SilverSim.BackendHandlers.Robust.Helo
{
    #region Service implementation
    class RobustHeloServerHandler : IPlugin
    {
        protected static readonly ILog m_Log = LogManager.GetLogger("ROBUST HELO HANDLER");
        private BaseHttpServer m_HttpServer;
        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);

        public RobustHeloServerHandler()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing HELO handler");
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/helo", HeloHandler);
        }

        public void HeloHandler(HttpRequest req)
        {
            switch (req.Method)
            {
                case "GET": case "HEAD":
                    HttpResponse res = req.BeginResponse();
                    res.ContentType = "text/plain";
                    res.Headers.Add("X-Handlers-Provided", "opensim-robust");
                    res.Close();
                    break;

                default:
                    req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                    break;
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("HeloHandler")]
    public class RobustHeloHandlerFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST HELO HANDLER");
        public RobustHeloHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new RobustHeloServerHandler();
        }
    }
    #endregion
}
