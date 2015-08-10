// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelPropertiesRequestByID)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelPropertiesRequestByID : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 SequenceID = 0;
        public Int32 LocalID = 0;

        public ParcelPropertiesRequestByID()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelPropertiesRequestByID m = new ParcelPropertiesRequestByID();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SequenceID = p.ReadInt32();
            m.LocalID = p.ReadInt32();
            return m;
        }
    }
}
