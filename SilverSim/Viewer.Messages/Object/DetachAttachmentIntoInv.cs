// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.DetachAttachmentIntoInv)]
    [Reliable]
    [NotTrusted]
    public class DetachAttachmentIntoInv : Message
    {
        public UUID AgentID;
        public UUID ItemID;

        public DetachAttachmentIntoInv()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            DetachAttachmentIntoInv m = new DetachAttachmentIntoInv();

            m.AgentID = p.ReadUUID();
            m.ItemID = p.ReadUUID();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(ItemID);
        }
    }
}
