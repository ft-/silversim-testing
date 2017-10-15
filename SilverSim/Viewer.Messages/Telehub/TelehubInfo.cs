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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ObjectID);
            p.WriteStringLen8(ObjectName);
            p.WriteVector3f(TelehubPos);
            p.WriteLLQuaternion(TelehubRot);
            p.WriteUInt8((byte)SpawnPoints.Count);
            foreach(var spawn in SpawnPoints)
            {
                p.WriteVector3f(spawn);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new TelehubInfo
            {
                ObjectID = p.ReadUUID(),
                ObjectName = p.ReadStringLen8(),
                TelehubPos = p.ReadVector3f(),
                TelehubRot = p.ReadLLQuaternion()
            };
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.SpawnPoints.Add(p.ReadVector3f());
            }
            return m;
        }
    }
}
