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

        public static RequestImage Decode(UDPPacket p)
        {
            var m = new RequestImage()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID()
            };
            uint count = p.ReadUInt8();
            for (uint idx = 0; idx < count; ++idx)
            {
                m.RequestImageList.Add(new RequestImageEntry()
                {
                    ImageID = p.ReadUUID(),
                    DiscardLevel = p.ReadInt8(),
                    DownloadPriority = p.ReadFloat(),
                    Packet = p.ReadUInt32(),
                    Type = (ImageType)p.ReadUInt8()
                });
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
