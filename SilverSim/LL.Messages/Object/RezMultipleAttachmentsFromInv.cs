// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RezMultipleAttachmentsFromInv)]
    [Reliable]
    [NotTrusted]
    public class RezMultipleAttachmentsFromInv : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID CompoundMsgID;
        public byte TotalObjects;
        public bool FirstDetachAll;

        public struct ObjectDataS
        {
            public UUID ItemID;
            public UUID OwnerID;
            public AttachmentPoint AttachmentPoint;
            public UInt32 ItemFlags;
            public InventoryPermissionsMask GroupMask;
            public InventoryPermissionsMask EveryoneMask;
            public InventoryPermissionsMask NextOwnerMask;
            public string Name;
            public string Description;
        }

        public List<ObjectDataS> ObjectData = new List<ObjectDataS>();

        public RezMultipleAttachmentsFromInv()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RezMultipleAttachmentsFromInv m = new RezMultipleAttachmentsFromInv();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.CompoundMsgID = p.ReadUUID();
            m.TotalObjects = p.ReadUInt8();
            m.FirstDetachAll = p.ReadBoolean();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                ObjectDataS objData = new ObjectDataS();
                objData.ItemID = p.ReadUUID();
                objData.OwnerID = p.ReadUUID();
                objData.AttachmentPoint = (AttachmentPoint)p.ReadUInt8();
                objData.ItemFlags = p.ReadUInt32();
                objData.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
                objData.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
                objData.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
                objData.Name = p.ReadStringLen8();
                objData.Description = p.ReadStringLen8();
                m.ObjectData.Add(objData);
            }

            return m;
        }
    }
}
