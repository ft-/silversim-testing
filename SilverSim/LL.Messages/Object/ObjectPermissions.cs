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
