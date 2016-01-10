// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelDwellReply)]
    [Reliable]
    [Trusted]
    public class ParcelDwellReply : Message
    {
        public UUID AgentID;
        public Int32 LocalID;
        public UUID ParcelID;
        public double Dwell;

        public ParcelDwellReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteInt32(LocalID);
            p.WriteUUID(ParcelID);
            p.WriteFloat((float)Dwell);
        }
    }
}
