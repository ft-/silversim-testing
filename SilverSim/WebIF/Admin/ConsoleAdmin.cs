using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SilverSim.WebIF.Admin
{
    public class ConsoleAdmin : IPlugin
    {
        public ConsoleAdmin()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

            AdminWebIF webif = loader.GetAdminWebIF();
            webif.JsonMethods.Add("console.command", ConsoleCommand);
        }

        public class ConsoleAdminTty : TTY
        {
            StreamWriter m_StreamWriter;

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
                        ConsoleAdminTty tty = new ConsoleAdminTty(w);
                        CommandRegistry.ExecuteCommand(tty.GetCmdLine(cmd), tty);
                    }
                }
            }
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
