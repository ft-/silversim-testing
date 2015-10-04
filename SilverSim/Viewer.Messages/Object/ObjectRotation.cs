// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectRotation)]
    [Reliable]
    [NotTrusted]
    public class ObjectRotation : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public Quaternion Rotation;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public List<Data> ObjectData = new List<Data>();


        public ObjectRotation()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectRotation m = new ObjectRotation();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.Rotation = p.ReadLLQuaternion();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
