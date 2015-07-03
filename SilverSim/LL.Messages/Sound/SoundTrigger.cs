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

namespace SilverSim.LL.Messages.Sound
{
    [UDPMessage(MessageType.SoundTrigger)]
    [NotTrusted]
    public class SoundTrigger : Message
    {
        public UUID SoundID;
        public UUID OwnerID;
        public UUID ObjectID;
        public UUID ParentID;
        public GridVector GridPosition;
        public Vector3 Position;
        public double Gain;

        public SoundTrigger()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(SoundID);
            p.WriteUUID(OwnerID);
            p.WriteUUID(ObjectID);
            p.WriteUUID(ParentID);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteVector3f(Position);
            p.WriteFloat((float)Gain);
        }

        public static SoundTrigger Decode(UDPPacket p)
        {
            SoundTrigger m = new SoundTrigger();
            m.SoundID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.ParentID = p.ReadUUID();
            m.GridPosition.RegionHandle = p.ReadUInt64();
            m.Position = p.ReadVector3f();
            m.Gain = p.ReadFloat();

            return m;
        }
    }
}
