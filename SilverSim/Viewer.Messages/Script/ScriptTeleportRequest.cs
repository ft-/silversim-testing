﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public static Message Decode(UDPPacket p)
        {
            ScriptTeleportRequest m = new ScriptTeleportRequest();
            m.ObjectName = p.ReadStringLen8();
            m.SimName = p.ReadStringLen8();
            m.SimPosition = p.ReadVector3f();
            m.LookAt = p.ReadVector3f();
            return m;
        }
    }
}
