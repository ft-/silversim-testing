// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Economy
{
    [UDPMessage(MessageType.MoneyTransferRequest)]
    [Reliable]
    [NotTrusted]
    public class MoneyTransferRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID SourceID;
        public UUID DestID;
        public byte Flags;
        public Int32 Amount;
        public byte AggregatePermNextOwner;
        public byte AggregatePermInventory;
        public Int32 TransactionType;
        public string Description;

        public MoneyTransferRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MoneyTransferRequest m = new MoneyTransferRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SourceID = p.ReadUUID();
            m.DestID = p.ReadUUID();
            m.Flags = p.ReadUInt8();
            m.Amount = p.ReadInt32();
            m.AggregatePermNextOwner = p.ReadUInt8();
            m.AggregatePermInventory = p.ReadUInt8();
            m.TransactionType = p.ReadInt32();
            m.Description = p.ReadStringLen8();

            return m;
        }
    }
}
