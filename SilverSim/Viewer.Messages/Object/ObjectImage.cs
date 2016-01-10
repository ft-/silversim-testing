// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectImage)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ObjectImage : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct ObjectDataEntry
        {
            public UInt32 ObjectLocalID;
            public string MediaURL;
            public byte[] TextureEntry;
        }

        public List<ObjectDataEntry> ObjectData = new List<ObjectDataEntry>();

        public ObjectImage()
        {

        }

        public static ObjectImage Decode(UDPPacket p)
        {
            ObjectImage m = new ObjectImage();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint entrycnt = p.ReadUInt8();
            while(entrycnt-- != 0)
            {
                ObjectDataEntry d = new ObjectDataEntry();
                d.ObjectLocalID = p.ReadUInt32();
                d.MediaURL = p.ReadStringLen8();
                d.TextureEntry = p.ReadBytes(p.ReadUInt16());
                m.ObjectData.Add(d);
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (ObjectDataEntry d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteStringLen8(d.MediaURL);
                p.WriteUInt16((ushort)d.TextureEntry.Length);
                p.WriteBytes(d.TextureEntry);
            }
        }
    }
}
