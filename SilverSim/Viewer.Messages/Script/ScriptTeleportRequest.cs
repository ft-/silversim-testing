// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.ScriptTeleportRequest)]
    [Reliable]
    [Trusted]
    public class ScriptTeleportRequest : Message
    {
        public string ObjectName;
        public string SimName;
        public Vector3 SimPosition = Vector3.Zero;
        public Vector3 LookAt = Vector3.Zero;

        public ScriptTeleportRequest()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteStringLen8(ObjectName);
            p.WriteStringLen8(SimName);
            p.WriteVector3f(SimPosition);
            p.WriteVector3f(LookAt);
        }
    }
}
