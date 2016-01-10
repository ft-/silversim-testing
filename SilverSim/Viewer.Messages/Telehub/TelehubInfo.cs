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
    }
}
