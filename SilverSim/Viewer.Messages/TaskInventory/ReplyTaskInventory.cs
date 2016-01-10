// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.TaskInventory
{
    [UDPMessage(MessageType.ReplyTaskInventory)]
    [Reliable]
    [Trusted]
    public class ReplyTaskInventory : Message
    {
        public UUID TaskID;
        public Int16 Serial;
        public string Filename;


        public ReplyTaskInventory()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(TaskID);
            p.WriteInt16(Serial);
            p.WriteStringLen8(Filename);
        }
    }

    [UDPMessage(MessageType.ReplyTaskInventory)]
    [Reliable]
    [Trusted]
    public class ReplyTaskInventoryNone : Message
    {
        public UUID TaskID;
        public Int16 Serial;


        public ReplyTaskInventoryNone()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(TaskID);
            p.WriteInt16(Serial);
            p.WriteUInt8(0);
        }
    }
}
