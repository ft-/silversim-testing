// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelMediaCommandMessage)]
    [Reliable]
    [Trusted]
    public class ParcelMediaCommandMessage : Message
    {
        public UInt32 Flags;
        public UInt32 Command;
        public double Time;

        public ParcelMediaCommandMessage()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt32(Flags);
            p.WriteUInt32(Command);
            p.WriteFloat((float)Time);
        }
    }
}
