// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelBuyPass)]
    [Reliable]
    [NotTrusted]
    public class ParcelBuyPass : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Int32 LocalID;

        public ParcelBuyPass()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelBuyPass m = new ParcelBuyPass();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();

            return m;
        }
    }
}
