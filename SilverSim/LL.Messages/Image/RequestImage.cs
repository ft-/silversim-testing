/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Image
{
    [UDPMessage(MessageType.RequestImage)]
    [Reliable]
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
    }
}
