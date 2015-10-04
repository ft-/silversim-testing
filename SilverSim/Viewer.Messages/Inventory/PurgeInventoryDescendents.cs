// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.PurgeInventoryDescendents)]
    [Reliable]
    [NotTrusted]
    public class PurgeInventoryDescendents : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID FolderID;

        public PurgeInventoryDescendents()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            PurgeInventoryDescendents m = new PurgeInventoryDescendents();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.FolderID = p.ReadUUID();

            return m;
        }
    }
}
