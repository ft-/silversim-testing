// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectScale)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    [UDPDeprecated]
    public class ObjectScale : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct ObjectDataEntry
        {
            public UInt32 ObjectLocalID;
            public Vector3 Size;
        }

        public List<ObjectDataEntry> ObjectData = new List<ObjectDataEntry>();

        public ObjectScale()
        {

        }

        public static ObjectScale Decode(UDPPacket p)
        {
            ObjectScale m = new ObjectScale();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint cnt = p.ReadUInt8();
            while(cnt-- != 0)
            {
                ObjectDataEntry d = new ObjectDataEntry();
                d.ObjectLocalID = p.ReadUInt32();
                d.Size = p.ReadVector3f();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
