// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.TransferInfo)]
    [Reliable]
    [Trusted]
    public class TransferInfo : Message
    {
        public UUID TransferID;
        public Int32 ChannelType;
        public Int32 TargetType;
        public Int32 Status;
        public Int32 Size;
        public byte[] Params = new byte[0];

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(TransferID);
            p.WriteInt32(ChannelType);
            p.WriteInt32(TargetType);
            p.WriteInt32(Status);
            p.WriteInt32(Size);
            p.WriteUInt16((ushort)Params.Length);
            p.WriteBytes(Params);
        }

        public static Message Decode(UDPPacket p)
        {
            return new TransferInfo()
            {
                TransferID = p.ReadUUID(),
                ChannelType = p.ReadInt32(),
                TargetType = p.ReadInt32(),
                Status = p.ReadInt32(),
                Size = p.ReadInt32(),
                Params = p.ReadBytes(p.ReadUInt16())
            };
        }
    }
}
