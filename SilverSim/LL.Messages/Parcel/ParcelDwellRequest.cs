// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelDwellRequest)]
    [Reliable]
    [NotTrusted]
    public class ParcelDwellRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Int32 LocalID;
        public UUID ParcelID;

        public ParcelDwellRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelDwellRequest m = new ParcelDwellRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.ParcelID = p.ReadUUID();

            return m;
        }
    }
}
