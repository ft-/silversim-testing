// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        void Cap_SimConsoleAsync(HttpRequest httpreq)
        {
            IValue iv;
            if (!httpreq.CallerIP.Equals(RemoteEndPoint))
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                iv = LlsdXml.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            if (!(iv is AString))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            string message = iv.ToString();
            SimConsoleAsyncTTY tty = new SimConsoleAsyncTTY(this);
            if (!Scene.IsSimConsoleAllowed(Agent.Owner))
            {
                tty.WriteFormatted(this.GetLanguageString(Agent.CurrentCulture, "SimConsoleNotAllowedForAgent", "SimConsole not allowed") + "\n", Agent.Owner.FirstName, Agent.Owner.LastName);
            }
            else
            {
                m_Commands.ExecuteCommand(tty.GetCmdLine(message), tty, Scene.ID);
                if(!tty.HaveOutputSent)
                {
                    tty.Write("");
                }
            }

            using (HttpResponse res = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                res.ContentType = "application/llsd+xml";
                using (Stream o = res.GetOutputStream())
                {
                    LlsdXml.Serialize(new BinaryData(new byte[1] { 0 }), o);
                }
            }
        }

        sealed class SimConsoleAsyncTTY : TTY
        {
            readonly AgentCircuit m_Circuit;
            public bool HaveOutputSent { get; private set; }
            public SimConsoleAsyncTTY(AgentCircuit c)
            {
                m_Circuit = c;
            }

            public override void Write(string text)
            {
                Messages.Console.SimConsoleResponse res = new Messages.Console.SimConsoleResponse();
                res.Message = text;
                m_Circuit.SendMessage(res);
                HaveOutputSent = true;
            }
        }
    }
}
