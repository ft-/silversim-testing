// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Appearance
{
    [UDPMessage(MessageType.AgentWearablesUpdate)]
    [Reliable]
    [Trusted]
    public class AgentWearablesUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 SerialNum;

        public struct WearableDataEntry
        {
            public UUID ItemID;
            public UUID AssetID;
            public SilverSim.Types.Asset.Format.WearableType WearableType;
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public AgentWearablesUpdate()
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
                p.WriteUUID(d.ItemID);
                p.WriteUUID(d.AssetID);
                p.WriteUInt8((byte)d.WearableType);
            }
        }
    }
}
