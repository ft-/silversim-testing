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
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectPermissions)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ObjectPermissions : Message
    {
        [Flags]
        public enum ChangeFieldMask : byte
        {
            Base = 0x01,
            Owner = 0x02,
            Group = 0x04,
            Everyone = 0x08,
            NextOwner = 0x10
        }

        public enum ChangeType : byte
        {
            Clear = 0,
            Set = 1
        }

        public struct Data
        {
            public UInt32 ObjectLocalID;
            public ChangeFieldMask Field;
            public ChangeType ChangeType;
            public InventoryPermissionsMask Mask;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public bool HasGodBit;

        public List<Data> ObjectData = new List<Data>();

        public static Message Decode(UDPPacket p)
        {
            var m = new ObjectPermissions
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),

                HasGodBit = p.ReadBoolean()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectData.Add(new Data
                {
                    ObjectLocalID = p.ReadUInt32(),
                    Field = (ChangeFieldMask)p.ReadUInt8(),
                    ChangeType = p.ReadUInt8() != 0 ? ChangeType.Set : ChangeType.Clear,
                    Mask = (InventoryPermissionsMask)p.ReadUInt32()
                });
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteBoolean(HasGodBit);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (var d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteUInt8((byte)d.Field);
                p.WriteUInt8(d.ChangeType == ChangeType.Set ? (byte)1 : (byte)0);
                p.WriteUInt32((uint)d.Mask);
            }
        }
    }
}
