// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Appearance
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
                m.WearableData.Add(d);
            }

            c = p.ReadUInt16();
            m.ObjectData = p.ReadBytes((int)c);

            c = p.ReadUInt8();
            m.VisualParams = p.ReadBytes((int)c);

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(SerialNum);
            p.WriteVector3f(Size);

            p.WriteUInt8((byte)WearableData.Count);
            foreach(WearableDataEntry d in WearableData)
            {
                p.WriteUUID(d.CacheID);
                p.WriteUInt8(d.TextureIndex);
            }

            p.WriteUInt16((ushort)ObjectData.Length);
            p.WriteBytes(ObjectData);

            p.WriteUInt8((byte)VisualParams.Length);
            p.WriteBytes(VisualParams);
        }
    }
}
