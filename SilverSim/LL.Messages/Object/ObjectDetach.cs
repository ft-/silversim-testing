// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectDetach)]
    [Reliable]
    [NotTrusted]
    public class ObjectDetach : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public List<UInt32> ObjectList = new List<UInt32>();

        public ObjectDetach()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectDetach m = new ObjectDetach();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectList.Add(p.ReadUInt32());
            }
            return m;
        }
    }
}
