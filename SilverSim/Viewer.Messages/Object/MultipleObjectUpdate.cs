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
    [UDPMessage(MessageType.MultipleObjectUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class MultipleObjectUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        [Flags]
        public enum UpdateFlags : byte
        {
            None = 0,
            UpdatePosition = 1,
            UpdateRotation = 2,
            UpdateScale = 4,
            UnknownFlagBit3 = 8,
            UnknownFlagBit4 = 16,
        }

        public struct ObjectDataEntry
        {
            public UInt32 ObjectLocalID;
            public UpdateFlags Flags;
            public byte[] Data;
        }

        public List<ObjectDataEntry> ObjectData = new List<ObjectDataEntry>();

        public static MultipleObjectUpdate Decode(UDPPacket p)
        {
            var m = new MultipleObjectUpdate
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID()
            };
            uint objcnt = p.ReadUInt8();
            while(objcnt-- != 0)
            {
                m.ObjectData.Add(new ObjectDataEntry
                {
                    ObjectLocalID = p.ReadUInt32(),
                    Flags = (UpdateFlags)p.ReadUInt8(),
                    Data = p.ReadBytes(p.ReadUInt8())
                });
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach(var d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteUInt8((byte)d.Flags);
                p.WriteUInt8((byte)d.Data.Length);
                p.WriteBytes(d.Data);
            }
        }
    }
}
