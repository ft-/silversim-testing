// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Appearance
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
            public WearableType WearableType;
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public AgentWearablesUpdate()
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
                p.WriteUUID(d.ItemID);
                p.WriteUUID(d.AssetID);
                p.WriteUInt8((byte)d.WearableType);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AgentWearablesUpdate m = new AgentWearablesUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SerialNum = p.ReadUInt32();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                WearableDataEntry d = new WearableDataEntry();
                d.ItemID = p.ReadUUID();
                d.AssetID = p.ReadUUID();
                d.WearableType = (WearableType)p.ReadUInt8();
                m.WearableData.Add(d);
            }
            return m;
        }
    }
}
