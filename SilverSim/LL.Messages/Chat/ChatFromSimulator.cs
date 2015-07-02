/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;

namespace SilverSim.LL.Messages.Chat
{
    [UDPMessage(MessageType.ChatFromSimulator)]
    [Reliable]
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
            p.WriteMessageType(Number);
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
