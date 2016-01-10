// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.RemoveInventoryFolder)]
    [Reliable]
    [NotTrusted]
    [EventQueueGet("RemoveInventoryFolder")]
    public class RemoveInventoryFolder : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public List<UUID> FolderData = new List<UUID>();

        public RemoveInventoryFolder()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RemoveInventoryFolder m = new RemoveInventoryFolder();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.FolderData.Add(p.ReadUUID());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)FolderData.Count);
            foreach(UUID folderid in FolderData)
            {
                p.WriteUUID(folderid);
            }
        }

        public override IValue SerializeEQG()
        {
            MapType llsd = new MapType();
            AnArray agentDataArray = new AnArray();
            MapType agentData = new MapType();
            agentData.Add("AgentID", AgentID);
            agentData.Add("SessionID", SessionID);
            agentDataArray.Add(agentData);
            llsd.Add("AgentData", agentDataArray);

            AnArray folderDataArray = new AnArray();

            foreach (UUID folder in FolderData)
            {
                MapType folderData = new MapType();
                folderData.Add("FolderID", folder);
                folderData.Add("AgentID", AgentID);
                folderDataArray.Add(folderData);
            }
            llsd.Add("FolderData", folderDataArray);

            return llsd;
        }
    }
}
