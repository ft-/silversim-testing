// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Agent;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectAttach)]
    [Reliable]
    [NotTrusted]
    public class ObjectAttach : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public Quaternion Rotation;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public AttachmentPoint AttachmentPoint;

        public List<Data> ObjectData = new List<Data>();

        public ObjectAttach()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectAttach m = new ObjectAttach();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.AttachmentPoint = (AttachmentPoint)p.ReadUInt8();

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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)AttachmentPoint);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach(Data d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteLLQuaternion(d.Rotation);
            }
        }
    }
}
