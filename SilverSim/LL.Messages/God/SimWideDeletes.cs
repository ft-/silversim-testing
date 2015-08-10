// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.God
{
    [UDPMessage(MessageType.SimWideDeletes)]
    [Reliable]
    [NotTrusted]
    public class SimWideDeletes : Message
    {
        [Flags]
        public enum DeleteFlags : uint
        {
            OthersLandOnly = 1 << 0,
            AlwaysReturnObjects = 1 << 1,
            ScriptedOnly = 1 << 2
        }

        public UUID AgentID;
        public UUID SessionID;
        public UUID TargetID;
        public DeleteFlags Flags;

        public SimWideDeletes()
        {

        }

        public static SimWideDeletes Decode(UDPPacket p)
        {
            SimWideDeletes m = new SimWideDeletes();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TargetID = p.ReadUUID();
            m.Flags = (DeleteFlags)p.ReadUInt32();
            return m;
        }
    }
}
