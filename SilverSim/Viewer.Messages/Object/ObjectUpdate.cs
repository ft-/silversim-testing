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
    [UDPMessage(MessageType.ObjectUpdate)]
    [Trusted]
    public class UnreliableObjectUpdate : Message
    {
        public class ObjData
        {
            public ObjData()
            {

            }

            public UInt32 LocalID;
            public byte State;
            public UUID FullID;
            public UInt32 CRC;
            public PrimitiveCode PCode;
            public PrimitiveMaterial Material;
            public ClickActionType ClickAction;
            public Vector3 Scale;
            public byte[] ObjectData;
            public UInt32 ParentID;
            public PrimitiveFlags UpdateFlags;
            public byte PathCurve;
            public byte ProfileCurve;
            public UInt16 PathBegin; // 0 to 1, quanta = 0.01
            public UInt16 PathEnd; // 0 to 1, quanta = 0.01
            public byte PathScaleX; // 0 to 1, quanta = 0.01
            public byte PathScaleY; // 0 to 1, quanta = 0.01
            public byte PathShearX; // -.5 to .5, quanta = 0.01
            public byte PathShearY; // -.5 to .5, quanta = 0.01
            public sbyte PathTwist; // -1 to 1, quanta = 0.01
            public sbyte PathTwistBegin; // -1 to 1, quanta = 0.01
            public sbyte PathRadiusOffset; // -1 to 1, quanta = 0.01
            public sbyte PathTaperX; // -1 to 1, quanta = 0.01
            public sbyte PathTaperY; // -1 to 1, quanta = 0.01
            public byte PathRevolutions; // 0 to 3, quanta = 0.015
            public sbyte PathSkew; // -1 to 1, quanta = 0.01
            public UInt16 ProfileBegin; // 0 to 1, quanta = 0.01
            public UInt16 ProfileEnd; // 0 to 1, quanta = 0.01
            public UInt16 ProfileHollow; // 0 to 1, quanta = 0.01
            public byte[] TextureEntry;
            public byte[] TextureAnim;
            public string NameValue;
            public byte[] Data;
            public string Text;
            public ColorAlpha TextColor;
            public string MediaURL;
            public byte[] PSBlock;
            public byte[] ExtraParams;
            public UUID LoopedSound;
            public UUID OwnerID;
            public double Gain;
            public PrimitiveSoundFlags Flags;
            public double Radius;
            public byte JointType;
            public Vector3 JointPivot;
            public Vector3 JointAxisOrAnchor;
        }

        public GridVector GridPosition;
        public UInt16 TimeDilation = 65535;

        public List<ObjData> ObjectData = new List<ObjData>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (ObjData d in ObjectData)
            {
                p.WriteUInt32(d.LocalID);
                p.WriteUInt8(d.State);
                p.WriteUUID(d.FullID);
                p.WriteUInt32(d.CRC);
                p.WriteUInt8((byte)d.PCode);
                p.WriteUInt8((byte)d.Material);
                p.WriteUInt8((byte)d.ClickAction);
                p.WriteVector3f(d.Scale);
                p.WriteUInt8((byte)d.ObjectData.Length);
                p.WriteBytes(d.ObjectData);
                p.WriteUInt32(d.ParentID);
                p.WriteUInt32((uint)d.UpdateFlags);
                p.WriteUInt8(d.PathCurve);
                p.WriteUInt8(d.ProfileCurve);
                p.WriteUInt16(d.PathBegin);
                p.WriteUInt16(d.PathEnd);
                p.WriteUInt8(d.PathScaleX);
                p.WriteUInt8(d.PathScaleY);
                p.WriteUInt8(d.PathShearX);
                p.WriteUInt8(d.PathShearY);
                p.WriteInt8(d.PathTwist);
                p.WriteInt8(d.PathTwistBegin);
                p.WriteInt8(d.PathRadiusOffset);
                p.WriteInt8(d.PathTaperX);
                p.WriteInt8(d.PathTaperY);
                p.WriteUInt8(d.PathRevolutions);
                p.WriteInt8(d.PathSkew);
                p.WriteUInt16(d.ProfileBegin);
                p.WriteUInt16(d.ProfileEnd);
                p.WriteUInt16(d.ProfileHollow);
                p.WriteUInt16((ushort)d.TextureEntry.Length);
                p.WriteBytes(d.TextureEntry);
                p.WriteUInt8((byte)d.TextureAnim.Length);
                p.WriteBytes(d.TextureAnim);
                p.WriteStringLen16(d.NameValue);
                p.WriteUInt16((ushort)d.Data.Length);
                p.WriteBytes(d.Data);
                p.WriteStringLen8(d.Text);
                p.WriteUInt8(d.TextColor.R_AsByte);
                p.WriteUInt8(d.TextColor.G_AsByte);
                p.WriteUInt8(d.TextColor.B_AsByte);
                p.WriteUInt8((byte)(255 - d.TextColor.A_AsByte));
                p.WriteStringLen8(d.MediaURL);
                p.WriteUInt8((byte)d.PSBlock.Length);
                p.WriteBytes(d.PSBlock);
                p.WriteUInt8((byte)d.ExtraParams.Length);
                p.WriteBytes(d.ExtraParams);
                p.WriteUUID(d.LoopedSound);
                if (d.LoopedSound == UUID.Zero)
                {
                    p.WriteUUID(UUID.Zero);
                }
                else
                {
                    p.WriteUUID(d.OwnerID);
                }
                p.WriteFloat((float)d.Gain);
                p.WriteUInt8((byte)d.Flags);
                p.WriteFloat((float)d.Radius);
                p.WriteUInt8(d.JointType);
                p.WriteVector3f(d.JointPivot);
                p.WriteVector3f(d.JointAxisOrAnchor);
            }
        }
    }

    [UDPMessage(MessageType.ObjectUpdate)]
    [Reliable]
    [Trusted]
    public class ObjectUpdate : UnreliableObjectUpdate
    {
        public static Message Decode(UDPPacket p)
        {
            ObjectUpdate m = new ObjectUpdate();
            m.GridPosition.RegionHandle = p.ReadUInt64();
            m.TimeDilation = p.ReadUInt16();
            uint n = p.ReadUInt8();
            while (n-- != 0)
            {
                ObjData d = new ObjData();
                d.LocalID = p.ReadUInt32();
                d.State = p.ReadUInt8();
                d.FullID = p.ReadUUID();
                d.CRC = p.ReadUInt32();
                d.PCode = (PrimitiveCode)p.ReadUInt8();
                d.Material = (PrimitiveMaterial)p.ReadUInt8();
                d.ClickAction = (ClickActionType)p.ReadUInt8();
                d.Scale = p.ReadVector3f();
                d.ObjectData = p.ReadBytes(p.ReadUInt8());
                d.ParentID = p.ReadUInt32();
                d.UpdateFlags = (PrimitiveFlags)p.ReadUInt32();
                d.PathCurve = p.ReadUInt8();
                d.ProfileCurve = p.ReadUInt8();
                d.PathBegin = p.ReadUInt16();
                d.PathEnd = p.ReadUInt16();
                d.PathScaleX = p.ReadUInt8();
                d.PathScaleY = p.ReadUInt8();
                d.PathShearX = p.ReadUInt8();
                d.PathShearY = p.ReadUInt8();
                d.PathTwist = p.ReadInt8();
                d.PathTwistBegin = p.ReadInt8();
                d.PathRadiusOffset = p.ReadInt8();
                d.PathTaperX = p.ReadInt8();
                d.PathTaperY = p.ReadInt8();
                d.PathRevolutions = p.ReadUInt8();
                d.PathSkew = p.ReadInt8();
                d.ProfileBegin = p.ReadUInt16();
                d.ProfileEnd = p.ReadUInt16();
                d.ProfileHollow = p.ReadUInt16();
                d.TextureEntry = p.ReadBytes(p.ReadUInt16());
                d.TextureAnim = p.ReadBytes(p.ReadUInt8());
                d.NameValue = p.ReadStringLen16();
                d.Data = p.ReadBytes(p.ReadUInt16());
                d.Text = p.ReadStringLen8();
                d.TextColor = new ColorAlpha();
                d.TextColor.R_AsByte = p.ReadUInt8();
                d.TextColor.G_AsByte = p.ReadUInt8();
                d.TextColor.B_AsByte = p.ReadUInt8();
                d.TextColor.A_AsByte = (byte)(255 - p.ReadUInt8());
                d.MediaURL = p.ReadStringLen8();
                d.PSBlock = p.ReadBytes(p.ReadUInt8());
                d.ExtraParams = p.ReadBytes(p.ReadUInt8());
                d.LoopedSound = p.ReadUUID();
                d.OwnerID = p.ReadUUID();
                d.Gain = p.ReadFloat();
                d.Flags = (PrimitiveSoundFlags)p.ReadUInt8();
                d.Radius = p.ReadFloat();
                d.JointType = p.ReadUInt8();
                d.JointPivot = p.ReadVector3f();
                d.JointAxisOrAnchor = p.ReadVector3f();
            }
            return m;
        }
    }
}
