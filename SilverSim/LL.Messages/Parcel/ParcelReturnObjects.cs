// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelReturnObjects)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelReturnObjects : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID = 0;
        public UInt32 ReturnType = 0;
        public List<UUID> TaskIDs = new List<UUID>();
        public List<UUID> OwnerIDs = new List<UUID>();

        public ParcelReturnObjects()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelReturnObjects m = new ParcelReturnObjects();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.ReturnType = p.ReadUInt32();

            uint cnt;
            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.TaskIDs.Add(p.ReadUUID());
            }

            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.OwnerIDs.Add(p.ReadUUID());
            }
            return m;
        }
    }
}
