// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;

namespace SilverSim.WebIF.Admin
{
    [Description("WebIF Console Admin Support")]
    public class ConsoleAdmin : IPlugin, IPluginShutdown
    {
        AdminWebIF m_WebIF;
        CommandRegistry m_Commands;

        public ConsoleAdmin()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Commands = loader.CommandRegistry;
            m_WebIF = loader.GetAdminWebIF();
            m_WebIF.JsonMethods.Add("console.command", ConsoleCommand);
        }

        public class ConsoleAdminTty : TTY
        {
            readonly StreamWriter m_StreamWriter;

            public ConsoleAdminTty(StreamWriter w)
            {
                m_StreamWriter = w;
            }

            public override void Write(string s)
            {
                m_StreamWriter.Write(s);
            }
        }

        static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        [AdminWebIF.RequiredRight("console.access")]
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
                    using (StreamWriter w = new StreamWriter(o, UTF8NoBOM))
                    {
                        AdminWebIF webif = m_WebIF;
                        if (webif != null)
                        {
                            ConsoleAdminTty tty = new ConsoleAdminTty(w);
                            tty.SelectedScene = webif.GetSelectedRegion(req, jsondata);
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
        public ConsoleAdminFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ConsoleAdmin();
        }
    }
    #endregion
}
