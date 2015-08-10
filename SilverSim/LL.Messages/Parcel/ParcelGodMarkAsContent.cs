// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelGodMarkAsContent)]
    [Reliable]
    [NotTrusted]
    public class ParcelGodMarkAsContent : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Int32 LocalID;

        public ParcelGodMarkAsContent()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelGodMarkAsContent m = new ParcelGodMarkAsContent();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();

            return m;
        }
    }
}
