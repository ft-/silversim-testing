using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Object
{
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

        public PrimitiveCode PCode = PrimitiveCode.None;
        public PrimitiveMaterial Material = PrimitiveMaterial.Wood;
        public UInt32 AddFlags = 0;
        public byte PathCurve = 0;
        public byte ProfileCurve = 0;
        public UInt16 PathBegin = 0;
        public UInt16 PathEnd = 0;
        public byte PathScaleX = 0;
        public byte PathScaleY = 0;
        public byte PathShearX = 0;
        public byte PathShearY = 0;
        public sbyte PathTwist = 0;
        public sbyte PathTwistBegin = 0;
        public sbyte PathRadiusOffset = 0;
        public sbyte PathTaperX = 0;
        public sbyte PathTaperY = 0;
        public byte PathRevolutions = 0;
        public sbyte PathSkew = 0;
        public UInt16 ProfileBegin = 0;
        public UInt16 ProfileEnd = 0;
        public UInt16 ProfileHollow = 0;
        public bool BypassRaycast = false;
        public Vector3 RayStart = Vector3.Zero;
        public Vector3 RayEnd = Vector3.Zero;
        public UUID RayTargetID = UUID.Zero;
        public bool RayEndIsIntersection = false;
        public Vector3 Scale = Vector3.One;
        public Quaternion Rotation = Quaternion.Identity;
        public byte State = 0;
        public AttachmentPoint LastAttachPoint = AttachmentPoint.NotAttached;

        /* Extension for internal handling */
        public InventoryPermissionsMask BasePermissions = InventoryPermissionsMask.All; 
        public InventoryPermissionsMask EveryOnePermissions = InventoryPermissionsMask.None;
        public InventoryPermissionsMask CurrentPermissions = InventoryPermissionsMask.Every;
        public InventoryPermissionsMask NextOwnerPermissions = InventoryPermissionsMask.None;
        public InventoryPermissionsMask GroupPermissions = InventoryPermissionsMask.None; 

        public ObjectAdd()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.ObjectAdd;
            }
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
    }
}
