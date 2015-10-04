// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelMediaUpdate)]
    [Reliable]
    [Trusted]
    public class ParcelMediaUpdate : Message
    {
        public string MediaURL;
        public UUID MediaID;
        public bool MediaAutoScale;
        public string MediaType;
        public string MediaDesc;
        public Int32 MediaWidth;
        public Int32 MediaHeight;
        public byte MediaLoop;

        public ParcelMediaUpdate()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteStringLen8(MediaURL);
            p.WriteUUID(MediaID);
            p.WriteBoolean(MediaAutoScale);
            p.WriteStringLen8(MediaType);
            p.WriteStringLen8(MediaDesc);
            p.WriteInt32(MediaWidth);
            p.WriteInt32(MediaHeight);
            p.WriteUInt8(MediaLoop);
        }
    }
}
