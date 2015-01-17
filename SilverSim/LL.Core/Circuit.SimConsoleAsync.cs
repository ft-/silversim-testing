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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System.Net;
using System.Xml;
using System.IO;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types.Inventory;
using SilverSim.Types.Asset;
using ThreadedClasses;
using SilverSim.LL.Messages;
using SilverSim.Main.Common.CmdIO;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void Cap_SimConsoleAsync(HttpRequest httpreq)
        {
            IValue iv;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                iv = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
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
                tty.WriteFormatted("SimConsole not allowed for agent {0} {1}\n", Agent.Owner.FirstName, Agent.Owner.LastName);
            }
            else
            {
                CommandRegistry.ExecuteCommand(tty.GetCmdLine(message), tty, Scene.ID);
            }

            HttpResponse res;
            res = httpreq.BeginResponse(HttpStatusCode.OK, "OK");
            res.ContentType = "application/llsd+xml";
            Stream o = res.GetOutputStream();
            LLSD_XML.Serialize(new BinaryData(new byte[1] { 0 }), o);
            res.Close();

        }

        class SimConsoleAsyncTTY : TTY
        {
            Circuit m_Circuit;
            public SimConsoleAsyncTTY(Circuit c)
            {
                m_Circuit = c;
            }

            public override void Write(string text)
            {
                Messages.Console.SimConsoleResponse res = new Messages.Console.SimConsoleResponse();
                res.Message = text;
                m_Circuit.SendMessage(res);
            }
        }
    }
}
