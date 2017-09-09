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
    [UDPMessage(MessageType.ObjectDuplicateOnRay)]
    [Reliable]
    [NotTrusted]
    [Zerocoded]
    public class ObjectDuplicateOnRay : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public Vector3 RayStart = Vector3.Zero;
        public Vector3 RayEnd = Vector3.Zero;
        public bool BypassRayCast;
        public bool RayEndIsIntersection;
        public bool CopyCenters;
        public bool CopyRotates;
        public UUID RayTargetID = UUID.Zero;
        public PrimitiveFlags DuplicateFlags;
        public List<UInt32> ObjectLocalIDs = new List<uint>();

        public static Message Decode(UDPPacket p)
        {
            var m = new ObjectDuplicateOnRay()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                RayStart = p.ReadVector3f(),
                RayEnd = p.ReadVector3f(),
                BypassRayCast = p.ReadBoolean(),
                RayEndIsIntersection = p.ReadBoolean(),
                CopyCenters = p.ReadBoolean(),
                CopyRotates = p.ReadBoolean(),
                RayTargetID = p.ReadUUID(),
                DuplicateFlags = (PrimitiveFlags)p.ReadUInt32()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectLocalIDs.Add(p.ReadUInt32());
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteVector3f(RayStart);
            p.WriteVector3f(RayEnd);
            p.WriteBoolean(BypassRayCast);
            p.WriteBoolean(RayEndIsIntersection);
            p.WriteBoolean(CopyCenters);
            p.WriteBoolean(CopyRotates);
            p.WriteUUID(RayTargetID);
            p.WriteUInt32((uint)DuplicateFlags);
            p.WriteUInt8((byte)ObjectLocalIDs.Count);
            foreach (UInt32 d in ObjectLocalIDs)
            {
                p.WriteUInt32(d);
            }
        }
    }
}
