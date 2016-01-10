// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public MultipleObjectUpdate()
        {

        }

        public static MultipleObjectUpdate Decode(UDPPacket p)
        {
            MultipleObjectUpdate m = new MultipleObjectUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint objcnt = p.ReadUInt8();
            while(objcnt-- != 0)
            {
                ObjectDataEntry d = new ObjectDataEntry();
                d.ObjectLocalID = p.ReadUInt32();
                d.Flags = (UpdateFlags)p.ReadUInt8();
                d.Data = p.ReadBytes(p.ReadUInt8());
                m.ObjectData.Add(d);
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach(ObjectDataEntry d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteUInt8((byte)d.Flags);
                p.WriteUInt8((byte)d.Data.Length);
                p.WriteBytes(d.Data);
            }
        }
    }
}
