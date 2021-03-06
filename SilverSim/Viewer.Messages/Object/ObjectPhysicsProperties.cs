﻿// SilverSim is distributed under the terms of the
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
using System.Collections.Generic;
using System.Linq;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Object
{
    [EventQueueGet("ObjectPhysicsProperties")]
    public class ObjectPhysicsProperties : Message
    {
        public struct ObjectDataEntry
        {
            public uint LocalID;
            public PrimitivePhysicsShapeType PhysicsShapeType;
            public double Density;
            public double Friction;
            public double Restitution;
            public double GravityMultiplier;
        }

        public List<ObjectDataEntry> ObjectData = new List<ObjectDataEntry>();

        public override IValue SerializeEQG()
        {
            var objectdata = new AnArray();

            foreach(ObjectDataEntry e in ObjectData)
            {
                objectdata.Add(new MapType
                {
                    { "LocalID", (int)e.LocalID },
                    { "PhysicsShapeType", (int)e.PhysicsShapeType },
                    { "Density", e.Density },
                    { "Friction", e.Friction },
                    { "Restitution", e.Restitution },
                    { "GravityMultiplier", e.GravityMultiplier }
                });
            }

            return new MapType
            {
                ["ObjectData"] = objectdata
            };
        }

        public static Message DeserializeEQG(IValue value)
        {
            var objData = (AnArray)((MapType)value)["ObjectData"];
            var msg = new ObjectPhysicsProperties();
            foreach(MapType m in objData.OfType<MapType>())
            {
                msg.ObjectData.Add(new ObjectDataEntry
                {
                    LocalID = m["LocalID"].AsUInt,
                    PhysicsShapeType = (PrimitivePhysicsShapeType)m["PhysicsShapeType"].AsInt,
                    Friction = m["Friction"].AsReal,
                    Restitution = m["Restitution"].AsReal,
                    GravityMultiplier = m["GravityMultiplier"].AsReal
                });
            }
            return msg;
        }
    }
}
