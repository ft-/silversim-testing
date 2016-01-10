// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.FetchInventoryDescendents)]
    [Reliable]
    [NotTrusted]
    public class FetchInventoryDescendents : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID FolderID;
        public UUID OwnerID;
        public Int32 SortOrder;
        public bool FetchFolders;
        public bool FetchItems;

        public FetchInventoryDescendents()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            FetchInventoryDescendents m = new FetchInventoryDescendents();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.FolderID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.SortOrder = p.ReadInt32();
            m.FetchFolders = p.ReadBoolean();
            m.FetchItems = p.ReadBoolean();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(FolderID);
            p.WriteUUID(OwnerID);
            p.WriteInt32(SortOrder);
            p.WriteBoolean(FetchFolders);
            p.WriteBoolean(FetchItems);
        }
    }
}
