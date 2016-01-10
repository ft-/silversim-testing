// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Appearance
{
    [UDPMessage(MessageType.AgentIsNowWearing)]
    [Reliable]
    [NotTrusted]
    public class AgentIsNowWearing : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct WearableDataEntry
        {
            public UUID ItemID;
            public WearableType WearableType;
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public AgentIsNowWearing()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AgentIsNowWearing m = new AgentIsNowWearing();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                WearableDataEntry d = new WearableDataEntry();
                d.ItemID = p.ReadUUID();
                d.WearableType = (WearableType)p.ReadUInt8();
                m.WearableData.Add(d);
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)WearableData.Count);
            foreach(WearableDataEntry d in WearableData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUInt8((byte)d.WearableType);
            }
        }
    }
}
