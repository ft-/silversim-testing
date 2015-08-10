// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.Appearance
{
    [UDPMessage(MessageType.AgentSetAppearance)]
    [Reliable]
    [NotTrusted]
    public class AgentSetAppearance : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 SerialNum;
        public Vector3 Size;

        public struct WearableDataEntry
        {
            public UUID CacheID;
            public byte TextureIndex;
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public byte[] ObjectData = new byte[0];

        public byte[] VisualParams = new byte[0];

        public AgentSetAppearance()
        {

        }

        public static AgentSetAppearance Decode(UDPPacket p)
        {
            AgentSetAppearance m = new AgentSetAppearance();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SerialNum = p.ReadUInt32();
            m.Size = p.ReadVector3f();

            uint c = p.ReadUInt8();

            for (uint i = 0; i < c; ++i)
            {
                WearableDataEntry d = new WearableDataEntry();
                d.CacheID = p.ReadUUID();
                d.TextureIndex = p.ReadUInt8();
            }

            c = p.ReadUInt16();
            m.ObjectData = p.ReadBytes((int)c);

            c = p.ReadUInt8();
            m.VisualParams = p.ReadBytes((int)c);

            return m;
        }
    }
}
