// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Appearance
{
    [UDPMessage(MessageType.AgentCachedTextureResponse)]
    [Reliable]
    [Trusted]
    public class AgentCachedTextureResponse : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 SerialNum;

        public struct WearableDataEntry
        {
            public UUID TextureID;
            public byte TextureIndex;
            public string HostName;

            public WearableDataEntry(byte textureIndex, UUID textureID)
            {
                TextureIndex = textureIndex;
                TextureID = textureID;
                HostName = "";
            }
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public AgentCachedTextureResponse()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(SerialNum);

            p.WriteUInt8((byte)WearableData.Count);
            foreach (WearableDataEntry d in WearableData)
            {
                p.WriteUUID(d.TextureID);
                p.WriteUInt8(d.TextureIndex);
                p.WriteStringLen8(d.HostName);
            }
        }
    }
}
