// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Appearance
{
    [UDPMessage(MessageType.AgentCachedTexture)]
    [Reliable]
    [NotTrusted]
    public class AgentCachedTexture : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Int32 SerialNum;

        public struct WearableDataEntry
        {
            public UUID ID;
            public byte TextureIndex;
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public AgentCachedTexture()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AgentCachedTexture m = new AgentCachedTexture();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SerialNum = p.ReadInt32();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                WearableDataEntry d;
                d.ID = p.ReadUUID();
                d.TextureIndex = p.ReadUInt8();
                m.WearableData.Add(d);
            }

            return m;
        }
    }
}
