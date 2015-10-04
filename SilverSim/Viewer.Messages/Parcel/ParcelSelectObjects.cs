// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelSelectObjects)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelSelectObjects : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID = 0;
        public UInt32 ReturnType = 0;

        public List<UUID> ReturnIDs = new List<UUID>();

        public ParcelSelectObjects()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelSelectObjects m = new ParcelSelectObjects();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.ReturnType = p.ReadUInt32();

            uint cnt;
            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.ReturnIDs.Add(p.ReadUUID());
            }

            return m;
        }
    }
}
