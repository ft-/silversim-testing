// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Image
{
    [UDPMessage(MessageType.RequestImage)]
    [Reliable]
    [NotTrusted]
    public class RequestImage : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public enum ImageType : byte
        {
            Normal = 0,
            Baked = 1,
            ServerBaked = 2
        }

        public struct RequestImageEntry
        {
            public UUID ImageID;
            public sbyte DiscardLevel;
            public double DownloadPriority;
            public UInt32 Packet;
            public ImageType Type;
        }

        public readonly List<RequestImageEntry> RequestImageList = new List<RequestImageEntry>();

        public RequestImage()
        {

        }

        public static RequestImage Decode(UDPPacket p)
        {
            RequestImage m = new RequestImage();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint count = p.ReadUInt8();
            for (uint idx = 0; idx < count; ++idx)
            {
                RequestImageEntry e = new RequestImageEntry();
                e.ImageID = p.ReadUUID();
                e.DiscardLevel = p.ReadInt8();
                e.DownloadPriority = p.ReadFloat();
                e.Packet = p.ReadUInt32();
                e.Type = (ImageType)p.ReadUInt8();
                m.RequestImageList.Add(e);
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)RequestImageList.Count);
            foreach(RequestImageEntry e in RequestImageList)
            {
                p.WriteUUID(e.ImageID);
                p.WriteInt8(e.DiscardLevel);
                p.WriteFloat((float)e.DownloadPriority);
                p.WriteUInt32(e.Packet);
                p.WriteUInt8((byte)e.Type);
            }
        }
    }
}
