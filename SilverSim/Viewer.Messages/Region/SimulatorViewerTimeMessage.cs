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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(UsecSinceStart);
            p.WriteUInt32(SecPerDay);
            p.WriteUInt32(SecPerYear);
            p.WriteVector3f(SunDirection);
            p.WriteFloat((float)SunPhase);
            p.WriteVector3f(SunAngVelocity);
        }

        public static Message Decode(UDPPacket p)
        {
            return new SimulatorViewerTimeMessage()
            {
                UsecSinceStart = p.ReadUInt64(),
                SecPerDay = p.ReadUInt32(),
                SecPerYear = p.ReadUInt32(),
                SunDirection = p.ReadVector3f(),
                SunPhase = p.ReadFloat(),
                SunAngVelocity = p.ReadVector3f()
            };
        }
    }
}
