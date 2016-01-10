// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectOwner)]
    [Reliable]
    [NotTrusted]
    public class ObjectOwner : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public bool HasGodBit;

        public UUID OwnerID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public List<UInt32> ObjectList = new List<UInt32>();

        public ObjectOwner()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectOwner m = new ObjectOwner();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.HasGodBit = p.ReadBoolean();
            m.OwnerID = p.ReadUUID();
            m.GroupID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectList.Add(p.ReadUInt32());
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteBoolean(HasGodBit);
            p.WriteUUID(OwnerID);
            p.WriteUUID(GroupID);

            p.WriteUInt8((byte)ObjectList.Count);
            foreach (uint d in ObjectList)
            {
                p.WriteUInt32(d);
            }
        }
    }
}
