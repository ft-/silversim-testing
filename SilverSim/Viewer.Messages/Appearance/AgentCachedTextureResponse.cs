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

namespace SilverSim.Viewer.Messages.Appearance
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
                HostName = string.Empty;
            }
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public AgentCachedTextureResponse()
        {

        }

        public override void Serialize(UDPPacket p)
        {
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

        public static Message Decode(UDPPacket p)
        {
            AgentCachedTextureResponse m = new AgentCachedTextureResponse();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SerialNum = p.ReadUInt32();

            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                WearableDataEntry d = new WearableDataEntry();
                d.TextureID = p.ReadUUID();
                d.TextureIndex = p.ReadUInt8();
                d.HostName = p.ReadStringLen8();
            }

            return m;
        }
    }
}
