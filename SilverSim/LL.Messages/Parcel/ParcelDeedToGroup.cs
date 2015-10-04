// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelDeedToGroup)]
    [Reliable]
    [NotTrusted]
    public class ParcelDeedToGroup : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;
        public Int32 LocalID;

        public ParcelDeedToGroup()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelDeedToGroup m = new ParcelDeedToGroup();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.LocalID = p.ReadInt32();

            return m;
        }
    }
}
