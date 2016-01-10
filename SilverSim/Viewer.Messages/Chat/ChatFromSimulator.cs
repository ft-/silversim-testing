// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Chat
{
    [UDPMessage(MessageType.ChatFromSimulator)]
    [Reliable]
    [Trusted]
    public class ChatFromSimulator : Message
    {
        public string FromName = string.Empty;
        public UUID SourceID = UUID.Zero;
        public UUID OwnerID = UUID.Zero;
        public ChatSourceType SourceType = ChatSourceType.Object;
        public ChatType ChatType = ChatType.Say;
        public ChatAudibleLevel Audible = ChatAudibleLevel.Not;
        public Vector3 Position = Vector3.Zero;
        public string Message = string.Empty;

        public ChatFromSimulator()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteStringLen8(FromName);
            p.WriteUUID(SourceID);
            p.WriteUUID(OwnerID);
            p.WriteUInt8((byte)SourceType);
            p.WriteUInt8((byte)ChatType);
            p.WriteUInt8((byte)Audible);
            p.WriteVector3f(Position);
            p.WriteStringLen16(Message);
        }
    }
}
