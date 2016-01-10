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
        public bool MediaLoop;

        public ParcelMediaUpdate()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteStringLen8(MediaURL);
            p.WriteUUID(MediaID);
            p.WriteBoolean(MediaAutoScale);
            p.WriteStringLen8(MediaType);
            p.WriteStringLen8(MediaDesc);
            p.WriteInt32(MediaWidth);
            p.WriteInt32(MediaHeight);
            p.WriteBoolean(MediaLoop);
        }

        public static Message Decode(UDPPacket p)
        {
            ParcelMediaUpdate m = new ParcelMediaUpdate();
            m.MediaURL = p.ReadStringLen8();
            m.MediaID = p.ReadUUID();
            m.MediaAutoScale = p.ReadBoolean();
            m.MediaType = p.ReadStringLen8();
            m.MediaDesc = p.ReadStringLen8();
            m.MediaWidth = p.ReadInt32();
            m.MediaHeight = p.ReadInt32();
            m.MediaLoop = p.ReadBoolean();
            return m;
        }
    }
}
