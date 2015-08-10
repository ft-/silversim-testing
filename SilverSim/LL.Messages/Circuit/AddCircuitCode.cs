﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.Circuit
{
    [UDPMessage(MessageType.AddCircuitCode)]
    [Reliable]
    [Trusted]
    public class AddCircuitCode : Message
    {
        public UInt32 CircuitCode;
        public UUID SessionID;
        public UUID AgentID;
    }
}
