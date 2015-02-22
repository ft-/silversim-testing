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
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Object
{
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

        public override MessageType Number
        {
            get
            {
                return MessageType.RezMultipleAttachmentsFromInv;
            }
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
