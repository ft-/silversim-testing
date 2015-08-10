// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelSetOtherCleanTime)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelSetOtherCleanTime : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID = 0;
        public Int32 OtherCleanTime = 0;

        public ParcelSetOtherCleanTime()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelSetOtherCleanTime m = new ParcelSetOtherCleanTime();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.OtherCleanTime = p.ReadInt32();
            return m;
        }
    }
}
