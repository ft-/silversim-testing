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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace SilverSim.WebIF.Admin
{
    [Description("WebIF Console Admin Support")]
    public class ConsoleAdmin : IPlugin, IPluginShutdown
    {
        private IAdminWebIF m_WebIF;
        private CommandRegistry m_Commands;

        public void Startup(ConfigurationLoader loader)
        {
            m_Commands = loader.CommandRegistry;
            m_WebIF = loader.GetAdminWebIF();
            m_WebIF.ModuleNames.Add("console");
            m_WebIF.JsonMethods.Add("console.command", ConsoleCommand);
        }

        public class ConsoleAdminTty : TTY
        {
            private readonly StreamWriter m_StreamWriter;

            public ConsoleAdminTty(StreamWriter w)
            {
                m_StreamWriter = w;
            }

            public override void Write(string s)
            {
                m_StreamWriter.Write(s);
            }
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        [AdminWebIfRequiredRight("console.access")]
        public void ConsoleCommand(HttpRequest req, Map jsondata)
        {
            if(!jsondata.ContainsKey("command"))
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            string cmd = jsondata["command"].ToString();
            using (HttpResponse res = req.BeginResponse("text/plain"))
            {
                using (Stream o = res.GetOutputStream())
                {
                    using (StreamWriter w = o.UTF8StreamWriter())
                    {
                        IAdminWebIF webif = m_WebIF;
                        if (webif != null)
                        {
                            var tty = new ConsoleAdminTty(w)
                            {
                                SelectedScene = webif.GetSelectedRegion(req, jsondata)
                            };
                            m_Commands.ExecuteCommand(tty.GetCmdLine(cmd), tty);
                            webif.SetSelectedRegion(req, jsondata, tty.SelectedScene);
                        }
                    }
                }
            }
        }

        public void Shutdown()
        {
            m_WebIF = null;
        }
    }

    #region Factory
    [PluginName("ConsoleAdmin")]
    public class ConsoleAdminFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new ConsoleAdmin();
    }
    #endregion
}
