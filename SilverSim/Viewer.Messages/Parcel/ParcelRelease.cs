// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelRelease)]
    [Reliable]
    [NotTrusted]
    public class ParcelRelease : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public List<Int32> LocalIDs = new List<Int32>();

        public ParcelRelease()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelRelease m = new ParcelRelease();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.LocalIDs.Add(p.ReadInt32());
            }

            return m;
        }
    }
}
