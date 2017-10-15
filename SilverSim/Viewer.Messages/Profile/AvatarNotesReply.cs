// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarNotesReply)]
    [Reliable]
    [Trusted]
    public class AvatarNotesReply : Message
    {
        public UUID AgentID = UUID.Zero;

        public UUID TargetID = UUID.Zero;
        public string Notes;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(TargetID);
            p.WriteStringLen16(Notes);
        }

        public static Message Decode(UDPPacket p) => new AvatarNotesReply
        {
            AgentID = p.ReadUUID(),
            TargetID = p.ReadUUID(),
            Notes = p.ReadStringLen16()
        };
    }
}
