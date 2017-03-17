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
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectFlagUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ObjectFlagUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 ObjectLocalID;
        public bool UsePhysics;
        public bool IsTemporary;
        public bool IsPhantom;
        public bool CastsShadows;

        public struct ExtraPhysicsData
        {
            public PrimitivePhysicsShapeType PhysicsShapeType;
            public double Density;
            public double Friction;
            public double Restitution;
            public double GravityMultiplier;
        }

        public List<ExtraPhysicsData> ExtraPhysics = new List<ExtraPhysicsData>();

        public ObjectFlagUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectFlagUpdate m = new ObjectFlagUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectLocalID = p.ReadUInt32();
            m.UsePhysics = p.ReadBoolean();
            m.IsTemporary = p.ReadBoolean();
            m.IsPhantom = p.ReadBoolean();
            m.CastsShadows = p.ReadBoolean();

            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                ExtraPhysicsData data = new ExtraPhysicsData();
                data.PhysicsShapeType = (PrimitivePhysicsShapeType)p.ReadUInt8();
                data.Density = p.ReadFloat();
                data.Friction = p.ReadFloat();
                data.Restitution = p.ReadFloat();
                data.GravityMultiplier = p.ReadFloat();
                m.ExtraPhysics.Add(data);
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(ObjectLocalID);
            p.WriteBoolean(UsePhysics);
            p.WriteBoolean(IsTemporary);
            p.WriteBoolean(IsPhantom);
            p.WriteBoolean(CastsShadows);
            p.WriteUInt8((byte)ExtraPhysics.Count);
            foreach(ExtraPhysicsData data in ExtraPhysics)
            {
                p.WriteUInt8((byte)data.PhysicsShapeType);
                p.WriteFloat((float)data.Density);
                p.WriteFloat((float)data.Friction);
                p.WriteFloat((float)data.Restitution);
                p.WriteFloat((float)data.GravityMultiplier);
            }
        }
    }
}
