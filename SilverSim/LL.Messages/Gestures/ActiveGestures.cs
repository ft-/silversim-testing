﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Gestures
{
    [UDPMessage(MessageType.ActivateGestures)]
    [Reliable]
    [NotTrusted]
    public class ActivateGestures : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 Flags;

        public struct DataEntry
        {
            public UUID ItemID;
            public UUID AssetID;
            public UInt32 GestureFlags;
        }

        public List<DataEntry> Data = new List<DataEntry>();

        public ActivateGestures()
        {

        }

        public static ActivateGestures Decode(UDPPacket p)
        {
            ActivateGestures m = new ActivateGestures();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = p.ReadUInt32();
            uint c = p.ReadUInt8();
            for(uint i = 0; i < c; ++i)
            {
                DataEntry e = new DataEntry();
                e.ItemID = p.ReadUUID();
                e.AssetID = p.ReadUUID();
                e.GestureFlags = p.ReadUInt32();
                m.Data.Add(e);
            }

            return m;
        }
    }
}
