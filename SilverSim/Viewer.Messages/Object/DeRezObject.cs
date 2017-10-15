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
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.DeRezObject)]
    [Reliable]
    [NotTrusted]
    public class DeRezObject : Message
    {
        public enum DeRezAction : byte
        {
            /* Save into existing item in agent inventory */
            SaveIntoAgentInventory = 0,
            /* Take a copy of the item */
            TakeCopy = 1,
            /* Save derezzed item into task inventory */
            SaveIntoTaskInventory = 2,
            /* Attachment */
            Attachment = 3,
            /* Take the original of the item into agent inventory */
            Take = 4,
            /* Take a copy of the item when being a god */
            GodTakeCopy = 5,
            /* Delete an item and putting it into trash */
            DeleteToTrash = 6,
            /* Save Attachment to inventory */
            AttachmentToInv = 7,
            /* Save attachment to existing inventory */
            AttachmentExists = 8,
            /* Return to owner */
            ReturnToOwner = 9,
            /* Return to last owner */
            ReturnToLastOwner = 10
        }

        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;
        public DeRezAction Destination;
        public UUID DestinationID;
        public UUID TransactionID;
        public byte PacketCount;
        public byte PacketNumber;

        public List<UInt32> ObjectLocalIDs = new List<UInt32>();

        public static Message Decode(UDPPacket p)
        {
            var m = new DeRezObject
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                Destination = (DeRezAction)p.ReadUInt8(),
                DestinationID = p.ReadUUID(),
                TransactionID = p.ReadUUID(),
                PacketCount = p.ReadUInt8(),
                PacketNumber = p.ReadUInt8()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectLocalIDs.Add(p.ReadUInt32());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUInt8((byte)Destination);
            p.WriteUUID(DestinationID);
            p.WriteUUID(TransactionID);
            p.WriteUInt8(PacketCount);
            p.WriteUInt8(PacketNumber);

            p.WriteUInt8((byte)ObjectLocalIDs.Count);
            foreach(var id in ObjectLocalIDs)
            {
                p.WriteUInt32(id);
            }
        }
    }
}
