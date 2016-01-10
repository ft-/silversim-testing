// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Image
{
    [UDPMessage(MessageType.ImageNotInDatabase)]
    [Reliable]
    [Trusted]
    public class ImageNotInDatabase : Message
    {
        public UUID ID = UUID.Zero;

        public ImageNotInDatabase()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ID);
        }

        public static Message Decode(UDPPacket p)
        {
            ImageNotInDatabase m = new ImageNotInDatabase();
            m.ID = p.ReadUUID();
            return m;
        }
    }
}
