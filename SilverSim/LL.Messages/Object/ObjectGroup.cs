// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectGroup)]
    [Reliable]
    [NotTrusted]
    public class ObjectGroup : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID GroupID = UUID.Zero;

        public List<UInt32> ObjectList = new List<UInt32>();

        public ObjectGroup()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectGroup m = new ObjectGroup();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectList.Add(p.ReadUInt32());
            }
            return m;
        }
    }
}
