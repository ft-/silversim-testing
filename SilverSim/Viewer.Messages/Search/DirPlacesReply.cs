// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirPlacesReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class DirPlacesReply : Message
    {
        public UUID AgentID;
        public UUID QueryID;
        
        public struct QueryReplyData
        {
            public UUID ParcelID;
            public string Name;
            public bool ForSale;
            public bool Auction;
            public double Dwell;
            public UInt32 Status;
        }

        public List<QueryReplyData> QueryReplies = new List<QueryReplyData>();

        public DirPlacesReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUInt8((byte)QueryReplies.Count);
            foreach(QueryReplyData d in QueryReplies)
            {
                p.WriteUUID(d.ParcelID);
                p.WriteStringLen8(d.Name);
                p.WriteBoolean(d.ForSale);
                p.WriteBoolean(d.Auction);
                p.WriteFloat((float)d.Dwell);
            }

            p.WriteUInt8((byte)QueryReplies.Count);
            foreach (QueryReplyData d in QueryReplies)
            {
                p.WriteUInt32(d.Status);
            }
        }
    }
}
