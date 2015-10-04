// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectDeselect)]
    [Reliable]
    [NotTrusted]
    public class ObjectDeselect : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public List<UInt32> ObjectData = new List<UInt32>();

        public ObjectDeselect()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectDeselect m = new ObjectDeselect();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectData.Add(p.ReadUInt32());
            }
            return m;
        }
    }
}
