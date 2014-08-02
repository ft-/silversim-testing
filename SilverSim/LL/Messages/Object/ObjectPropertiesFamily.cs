/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;
using SilverSim.Types.Inventory;

namespace SilverSim.LL.Messages.Object
{
    public class ObjectPropertiesFamily : Message
    {
        public UInt32 RequestFlags;
        public UUID ObjectID = UUID.Zero;
        public UUID OwnerID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UInt32 BaseMask;
        public UInt32 OwnerMask;
        public UInt32 GroupMask;
        public UInt32 EveryoneMask;
        public UInt32 NextOwnerMask;
        public Int32 OwnershipCost;
        public InventoryItem.SaleInfoData.SaleType SaleType;
        public Int32 SalePrice;
        public UInt32 Category;
        public UUID LastOwnerID = UUID.Zero;
        public string Name;
        public string Description;

        public ObjectPropertiesFamily()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ObjectPropertiesFamily;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt32(RequestFlags);
            p.WriteUUID(ObjectID);
            p.WriteUUID(OwnerID);
            p.WriteUUID(GroupID);
            p.WriteUInt32(BaseMask);
            p.WriteUInt32(OwnerMask);
            p.WriteUInt32(GroupMask);
            p.WriteUInt32(EveryoneMask);
            p.WriteUInt32(NextOwnerMask);
            p.WriteInt32(OwnershipCost);
            p.WriteUInt8((byte)SaleType);
            p.WriteInt32(SalePrice);
            p.WriteUInt32(Category);
            p.WriteUUID(LastOwnerID);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
        }
    }
}
