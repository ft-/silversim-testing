// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.LoadURL)]
    [Reliable]
    [Trusted]
    public class LoadURL : Message
    {
        public string ObjectName;
        public UUID ObjectID = UUID.Zero;
        public UUID OwnerID = UUID.Zero;
        public bool OwnerIsGroup;
        public string Message = string.Empty;
        public string URL = string.Empty;

        public LoadURL()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteStringLen8(ObjectName);
            p.WriteUUID(ObjectID);
            p.WriteUUID(OwnerID);
            p.WriteBoolean(OwnerIsGroup);
            p.WriteStringLen8(Message);
            p.WriteStringLen8(URL);
        }

        public static Message Decode(UDPPacket p)
        {
            LoadURL m = new LoadURL();
            m.ObjectName = p.ReadStringLen8();
            m.ObjectID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.OwnerIsGroup = p.ReadBoolean();
            m.Message = p.ReadStringLen8();
            m.URL = p.ReadStringLen8();
            return m;
        }
    }
}
