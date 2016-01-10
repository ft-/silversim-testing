// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Sound
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
