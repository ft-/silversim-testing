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
using SilverSim.Types.Script;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.ScriptQuestion)]
    [Reliable]
    [Trusted]
    public class ScriptQuestion : Message
    {
        public UUID TaskID = UUID.Zero;
        public UUID ItemID = UUID.Zero;
        public string ObjectName;
        public string ObjectOwner;
        public ScriptPermissions Questions;
        public UUID ExperienceID = UUID.Zero;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(TaskID);
            p.WriteUUID(ItemID);
            p.WriteStringLen8(ObjectName);
            p.WriteStringLen8(ObjectOwner);
            p.WriteUInt32((uint)Questions);
            if (ExperienceID != UUID.Zero)
            {
                p.WriteUUID(ExperienceID);
            }
        }

        public static Message Decode(UDPPacket p) => new ScriptQuestion
        {
            TaskID = p.ReadUUID(),
            ItemID = p.ReadUUID(),
            ObjectName = p.ReadStringLen8(),
            ObjectOwner = p.ReadStringLen8(),
            Questions = (ScriptPermissions)p.ReadUInt32(),
            ExperienceID = p.ReadUUID(UUID.Zero)
        };
    }
}
