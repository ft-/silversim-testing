// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Circuit
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
