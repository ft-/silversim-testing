// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarClassifiedReply)]
    [Reliable]
    [Trusted]
    public class AvatarClassifiedReply : Message
    {
        public UUID AgentID;
        public UUID TargetID;

        public struct ClassifiedData
        {
            public UUID ClassifiedID;
            public string Name;
        }

        public List<ClassifiedData> Data = new List<ClassifiedData>();

        public AvatarClassifiedReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(TargetID);
            p.WriteUInt8((byte)Data.Count);
            foreach(ClassifiedData d in Data)
            {
                p.WriteUUID(d.ClassifiedID);
                p.WriteStringLen8(d.Name);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AvatarClassifiedReply m = new AvatarClassifiedReply();
            m.AgentID = p.ReadUUID();
            m.TargetID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                ClassifiedData d = new ClassifiedData();
                d.ClassifiedID = p.ReadUUID();
                d.Name = p.ReadStringLen8();
                m.Data.Add(d);
            }
            return m;
        }
    }
}
