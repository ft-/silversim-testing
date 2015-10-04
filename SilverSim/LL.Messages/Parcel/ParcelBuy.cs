// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelBuy)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelBuy : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public bool IsGroupOwned;
        public bool RemoveContribution;
        public Int32 LocalID;
        public bool IsFinal;
        public Int32 Price;
        public Int32 Area;

        public ParcelBuy()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelBuy m = new ParcelBuy();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.GroupID = p.ReadUUID();
            m.IsGroupOwned = p.ReadBoolean();
            m.RemoveContribution = p.ReadBoolean();
            m.LocalID = p.ReadInt32();
            m.IsFinal = p.ReadBoolean();
            m.Price = p.ReadInt32();
            m.Area = p.ReadInt32();

            return m;
        }
    }
}
