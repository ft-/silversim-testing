// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Console
{
    [EventQueueGet("SimConsoleResponse")]
    [Trusted]
    public class SimConsoleResponse : Message
    {
        public string Message;

        public SimConsoleResponse()
        {
        }

        public SimConsoleResponse(string message)
        {
            Message = message;
        }

        public override IValue SerializeEQG()
        {
            return new AString(Message);
        }
    }
}
