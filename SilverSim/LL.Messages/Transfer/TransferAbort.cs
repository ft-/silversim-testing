// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.Transfer
{
    [UDPMessage(MessageType.TransferAbort)]
    [Reliable]
    [NotTrusted]
    public class TransferAbort : Message
    {
        public UUID TransferID;
        public int ChannelType;

        public TransferAbort()
        {
            
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(TransferID);
            p.WriteInt32(ChannelType);
        }

        public static TransferAbort Decode(UDPPacket p)
        {
            TransferAbort m = new TransferAbort();
            m.TransferID = p.ReadUUID();
            m.ChannelType = p.ReadInt32();

            return m;
        }
    }
}
