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
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectAdd)]
    [Reliable]
    [NotTrusted]
    public class ObjectAdd : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public Quaternion Rotation;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public PrimitiveCode PCode;
        public PrimitiveMaterial Material = PrimitiveMaterial.Wood;
        public UInt32 AddFlags;
        public byte PathCurve;
        public byte ProfileCurve;
        public UInt16 PathBegin;
        public UInt16 PathEnd;
        public byte PathScaleX;
        public byte PathScaleY;
        public byte PathShearX;
        public byte PathShearY;
        public sbyte PathTwist;
        public sbyte PathTwistBegin;
        public sbyte PathRadiusOffset;
        public sbyte PathTaperX;
        public sbyte PathTaperY;
        public byte PathRevolutions;
        public sbyte PathSkew;
        public UInt16 ProfileBegin;
        public UInt16 ProfileEnd;
        public UInt16 ProfileHollow;
        public bool BypassRaycast;
        public Vector3 RayStart = Vector3.Zero;
        public Vector3 RayEnd = Vector3.Zero;
        public UUID RayTargetID = UUID.Zero;
        public bool RayEndIsIntersection;
        public Vector3 Scale = Vector3.One;
        public Quaternion Rotation = Quaternion.Identity;
        public byte State;
        public AttachmentPoint LastAttachPoint;

        /* Extension for internal handling */
        public InventoryPermissionsMask BasePermissions = InventoryPermissionsMask.All; 
        public InventoryPermissionsMask EveryOnePermissions;
        public InventoryPermissionsMask CurrentPermissions = InventoryPermissionsMask.Every;
        public InventoryPermissionsMask NextOwnerPermissions;
        public InventoryPermissionsMask GroupPermissions; 

        public ObjectAdd()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectAdd m = new ObjectAdd();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();

            m.PCode = (PrimitiveCode)p.ReadUInt8();
            m.Material = (PrimitiveMaterial)p.ReadUInt8();
            m.AddFlags = p.ReadUInt32();
            m.PathCurve = p.ReadUInt8();
            m.ProfileCurve = p.ReadUInt8();
            m.PathBegin = p.ReadUInt16();
            m.PathEnd = p.ReadUInt16();
            m.PathScaleX = p.ReadUInt8();
            m.PathScaleY = p.ReadUInt8();
            m.PathShearX = p.ReadUInt8();
            m.PathShearY = p.ReadUInt8();
            m.PathTwist = p.ReadInt8();
            m.PathTwistBegin = p.ReadInt8();
            m.PathRadiusOffset = p.ReadInt8();
            m.PathTaperX = p.ReadInt8();
            m.PathTaperY = p.ReadInt8();
            m.PathRevolutions = p.ReadUInt8();
            m.PathSkew = p.ReadInt8();
            m.ProfileBegin = p.ReadUInt16();
            m.ProfileEnd = p.ReadUInt16();
            m.ProfileHollow = p.ReadUInt16();
            m.BypassRaycast = 0 != p.ReadUInt8();
            m.RayStart = p.ReadVector3f();
            m.RayEnd = p.ReadVector3f();
            m.RayTargetID = p.ReadUUID();
            m.RayEndIsIntersection = 0 != p.ReadUInt8();
            m.Scale = p.ReadVector3f();
            m.Rotation = p.ReadLLQuaternion();
            m.State = p.ReadUInt8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);

            p.WriteUInt8((byte)PCode);
            p.WriteUInt8((byte)Material);
            p.WriteUInt32(AddFlags);
            p.WriteUInt8(PathCurve);
            p.WriteUInt8(ProfileCurve);
            p.WriteUInt16(PathBegin);
            p.WriteUInt16(PathEnd);
            p.WriteUInt8(PathScaleX);
            p.WriteUInt8(PathScaleY);
            p.WriteUInt8(PathShearX);
            p.WriteUInt8(PathShearY);
            p.WriteInt8(PathTwist);
            p.WriteInt8(PathTwistBegin);
            p.WriteInt8(PathRadiusOffset);
            p.WriteInt8(PathTaperX);
            p.WriteInt8(PathTaperY);
            p.WriteUInt8(PathRevolutions);
            p.WriteInt8(PathSkew);
            p.WriteUInt16(ProfileBegin);
            p.WriteUInt16(ProfileEnd);
            p.WriteUInt16(ProfileHollow);
            p.WriteUInt8(BypassRaycast ? (byte)1 : (byte)0);
            p.WriteVector3f(RayStart);
            p.WriteVector3f(RayEnd);
            p.WriteUUID(RayTargetID);
            p.WriteUInt8(RayEndIsIntersection ? (byte)1 : (byte)0);
            p.WriteVector3f(Scale);
            p.WriteLLQuaternion(Rotation);
            p.WriteUInt8(State);
        }
    }
}
