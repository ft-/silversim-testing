// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.PayPriceReply)]
    [Reliable]
    [Trusted]
    public class PayPriceReply : Message
    {
        public UUID ObjectID = UUID.Zero;
        public Int32 DefaultPayPrice;
        public List<Int32> ButtonData = new List<Int32>();

        public PayPriceReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ObjectID);
            p.WriteInt32(DefaultPayPrice);
            p.WriteUInt8((byte)ButtonData.Count);
            foreach (Int32 d in ButtonData)
            {
                p.WriteInt32(d);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            PayPriceReply m = new PayPriceReply();
            m.ObjectID = p.ReadUUID();
            m.DefaultPayPrice = p.ReadInt32();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.ButtonData.Add(p.ReadInt32());
            }
            return m;
        }
    }
}
