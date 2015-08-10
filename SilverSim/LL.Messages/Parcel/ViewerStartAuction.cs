// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ViewerStartAuction)]
    [Reliable]
    [NotTrusted]
    public class ViewerStartAuction : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Int32 LocalID;
        public UUID SnapshotID;


        public ViewerStartAuction()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ViewerStartAuction m = new ViewerStartAuction();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.SnapshotID = p.ReadUUID();

            return m;
        }
    }
}
