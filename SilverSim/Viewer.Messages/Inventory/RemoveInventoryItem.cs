// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.RemoveInventoryItem)]
    [Reliable]
    [NotTrusted]
    [EventQueueGet("RemoveInventoryItem")]
    public class RemoveInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public List<UUID> InventoryData = new List<UUID>();

        public RemoveInventoryItem()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RemoveInventoryItem m = new RemoveInventoryItem();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.InventoryData.Add(p.ReadUUID());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)InventoryData.Count);
            foreach(UUID folderid in InventoryData)
            {
                p.WriteUUID(folderid);
            }
        }

        public override IValue SerializeEQG()
        {
            MapType llsd = new MapType();
            MapType agentData = new MapType();
            AnArray agentDataArray = new AnArray();
            agentData.Add("AgentID", AgentID);
            agentData.Add("SessionID", SessionID);
            agentDataArray.Add(agentData);
            llsd.Add("AgentData", agentDataArray);

            AnArray itemDataArray = new AnArray();
            foreach(UUID itemID in InventoryData)
            {
                MapType itemData = new MapType();
                itemData.Add("ItemID", itemID);
                itemData.Add("AgentID", AgentID);
                itemDataArray.Add(itemData);
            }
            llsd.Add("InventoryData", itemDataArray);

            return llsd;
        }
    }
}
