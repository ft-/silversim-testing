// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Script
{
    [UDPMessage(MessageType.LoadURL)]
    [Reliable]
    [Trusted]
    public class LoadURL : Message
    {
        public string ObjectName;
        public UUID ObjectID = UUID.Zero;
        public UUID OwnerID = UUID.Zero;
        public bool OwnerIsGroup = false;
        public string Message;
        public string URL;

        public LoadURL()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteStringLen8(ObjectName);
            p.WriteUUID(ObjectID);
            p.WriteUUID(OwnerID);
            p.WriteBoolean(OwnerIsGroup);
            p.WriteStringLen8(Message);
            p.WriteStringLen8(URL);
        }
    }
}
