// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Appearance
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
            public SilverSim.Types.Asset.Format.WearableType WearableType;
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
                d.WearableType = (SilverSim.Types.Asset.Format.WearableType)p.ReadUInt8();
                m.WearableData.Add(d);
            }

            return m;
        }
    }
}
