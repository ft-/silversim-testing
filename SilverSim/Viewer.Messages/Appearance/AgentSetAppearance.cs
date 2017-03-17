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
