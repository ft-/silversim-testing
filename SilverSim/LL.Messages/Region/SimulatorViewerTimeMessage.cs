// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Region
{
    [UDPMessage(MessageType.SimulatorViewerTimeMessage)]
    [Trusted]
    public class SimulatorViewerTimeMessage : Message
    {
        public UInt64 UsecSinceStart;
        public UInt32 SecPerDay;
        public UInt32 SecPerYear;
        public Vector3 SunDirection;
        public double SunPhase;
        public Vector3 SunAngVelocity;

        public SimulatorViewerTimeMessage()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt64(UsecSinceStart);
            p.WriteUInt32(SecPerDay);
            p.WriteUInt32(SecPerYear);
            p.WriteVector3f(SunDirection);
            p.WriteFloat((float)SunPhase);
            p.WriteVector3f(SunAngVelocity);
        }
    }
}
