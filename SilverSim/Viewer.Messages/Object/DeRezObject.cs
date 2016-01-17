﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            /* Take a copy of the item being a god */
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

        public DeRezObject()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            DeRezObject m = new DeRezObject();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Destination = (DeRezAction)p.ReadUInt8();
            m.DestinationID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.PacketCount = p.ReadUInt8();
            m.PacketNumber = p.ReadUInt8();

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
            foreach(uint id in ObjectLocalIDs)
            {
                p.WriteUInt32(id);
            }
        }
    }
}
