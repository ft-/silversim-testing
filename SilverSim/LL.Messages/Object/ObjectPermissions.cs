// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
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

        public bool HasGodBit = false;

        public List<Data> ObjectData = new List<Data>();

        public ObjectPermissions()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectPermissions m = new ObjectPermissions();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.HasGodBit = p.ReadBoolean();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.Field = (ChangeFieldMask)p.ReadUInt8();
                d.ChangeType = p.ReadUInt8() != 0 ? ChangeType.Set : ChangeType.Clear;
                d.Mask = (InventoryPermissionsMask)p.ReadUInt32();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
