// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectSaleInfo)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ObjectSaleInfo : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public InventoryItem.SaleInfoData.SaleType SaleType;
            public Int32 SalePrice;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public List<Data> ObjectData = new List<Data>();

        public ObjectSaleInfo()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectSaleInfo m = new ObjectSaleInfo();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
                d.SalePrice = p.ReadInt32();
                m.ObjectData.Add(d);
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (Data d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteUInt8((byte)d.SaleType);
                p.WriteInt32(d.SalePrice);
            }
        }
    }
}
