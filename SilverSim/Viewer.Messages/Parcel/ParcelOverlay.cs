// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelOverlay)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class ParcelOverlay : Message
    {
        public Int32 SequenceID;
        public byte[] Data = new byte[0];

        public ParcelOverlay()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteInt32(SequenceID);
            p.WriteUInt16((UInt16)Data.Length);
            p.WriteBytes(Data);
        }

        public static Message Decode(UDPPacket p)
        {
            ParcelOverlay m = new ParcelOverlay();
            m.SequenceID = p.ReadInt32();
            m.Data = p.ReadBytes(p.ReadUInt16());
            return m;
        }
    }
}
