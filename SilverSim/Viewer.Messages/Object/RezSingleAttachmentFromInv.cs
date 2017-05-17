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
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RezSingleAttachmentFromInv)]
    [Reliable]
    [NotTrusted]
    public class RezSingleAttachmentFromInv : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID ItemID;
        public UUID OwnerID;
        public AttachmentPoint AttachmentPoint;
        public UInt32 ItemFlags;
        public InventoryPermissionsMask GroupMask;
        public InventoryPermissionsMask EveryoneMask;
        public InventoryPermissionsMask NextOwnerMask;
        public string Name;
        public string Description;

        public static Message Decode(UDPPacket p)
        {
            return new RezSingleAttachmentFromInv()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                ItemID = p.ReadUUID(),
                OwnerID = p.ReadUUID(),
                AttachmentPoint = (AttachmentPoint)p.ReadUInt8(),
                ItemFlags = p.ReadUInt32(),
                GroupMask = (InventoryPermissionsMask)p.ReadUInt32(),
                EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32(),
                NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                Name = p.ReadStringLen8(),
                Description = p.ReadStringLen8()
            };
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ItemID);
            p.WriteUUID(OwnerID);
            p.WriteUInt8((byte)AttachmentPoint);
            p.WriteUInt32(ItemFlags);
            p.WriteUInt32((uint)GroupMask);
            p.WriteUInt32((uint)EveryoneMask);
            p.WriteUInt32((uint)NextOwnerMask);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
        }
    }
}
