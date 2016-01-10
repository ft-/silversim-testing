// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectSelect)]
    [Reliable]
    [NotTrusted]
    public class ObjectSelect : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public List<UInt32> ObjectData = new List<UInt32>();

        public ObjectSelect()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectSelect m = new ObjectSelect();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectData.Add(p.ReadUInt32());
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (uint d in ObjectData)
            {
                p.WriteUInt32(d);
            }
        }
    }
}
