// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Telehub
{
    [UDPMessage(MessageType.TelehubInfo)]
    [Reliable]
    [Trusted]
    public class TelehubInfo : Message
    {
        public UUID ObjectID = UUID.Zero;
        public string ObjectName = string.Empty;
        public Vector3 TelehubPos = Vector3.Zero;
        public Quaternion TelehubRot = Quaternion.Identity;

        public List<Vector3> SpawnPoints = new List<Vector3>();

        public TelehubInfo()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ObjectID);
            p.WriteStringLen8(ObjectName);
            p.WriteVector3f(TelehubPos);
            p.WriteLLQuaternion(TelehubRot);
            p.WriteUInt8((byte)SpawnPoints.Count);
            foreach(Vector3 spawn in SpawnPoints)
            {
                p.WriteVector3f(spawn);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            TelehubInfo m = new TelehubInfo();
            m.ObjectID = p.ReadUUID();
            m.ObjectName = p.ReadStringLen8();
            m.TelehubPos = p.ReadVector3f();
            m.TelehubRot = p.ReadLLQuaternion();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.SpawnPoints.Add(p.ReadVector3f());
            }
            return m;
        }
    }
}
