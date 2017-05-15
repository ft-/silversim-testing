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

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupAccountDetailsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class GroupAccountDetailsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public int IntervalDays;
        public int CurrentInterval;
        public string StartDate = string.Empty;
        public string Description = string.Empty;
        public int Amount;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteInt32(IntervalDays);
            p.WriteInt32(CurrentInterval);
            p.WriteStringLen8(StartDate);
            p.WriteStringLen8(Description);
            p.WriteInt32(Amount);
        }

        public static Message Decode(UDPPacket p)
        {
            return new GroupAccountDetailsReply()
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                RequestID = p.ReadUUID(),
                IntervalDays = p.ReadInt32(),
                CurrentInterval = p.ReadInt32(),
                StartDate = p.ReadStringLen8(),
                Description = p.ReadStringLen8(),
                Amount = p.ReadInt32()
            };
        }
    }
}
