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

using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.IO;
using System.Net;
using SilverSim.Viewer.Messages.Console;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        void Cap_SimConsoleAsync(HttpRequest httpreq)
        {
            IValue iv;
            if (httpreq.CallerIP != RemoteIP)
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

            var message = iv.ToString();
            var tty = new SimConsoleAsyncTTY(this);
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

            using (var res = httpreq.BeginResponse(HttpStatusCode.OK, "OK"))
            {
                res.ContentType = "application/llsd+xml";
                using (var o = res.GetOutputStream())
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
                var res = new SimConsoleResponse()
                {
                    Message = text
                };
                m_Circuit.SendMessage(res);
                HaveOutputSent = true;
            }
        }
    }
}
