// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.BuyObjectInventory)]
    [Reliable]
    [NotTrusted]
    public class BuyObjectInventory : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID ObjectID = UUID.Zero;
        public UUID ItemID = UUID.Zero;
        public UUID FolderID = UUID.Zero;

        public BuyObjectInventory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            BuyObjectInventory m = new BuyObjectInventory();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.ItemID = p.ReadUUID();
            m.FolderID = p.ReadUUID();

            return m;
        }
    }
}
