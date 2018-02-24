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

using SilverSim.Scene.Types.Object.Localization;
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        public class PrimitiveShape : IEquatable<PrimitiveShape>
        {
            #region Constructor
            public PrimitiveShape()
            {
            }

            public PrimitiveShape(PrimitiveShape c)
            {
                CopyFrom(c);
            }
            #endregion

            #region Fields
            public UUID SculptMap = UUID.Zero; /* 0 */
            public PrimitiveSculptType SculptType = PrimitiveSculptType.None; /* byte / 17 */
            public bool IsSculptInverted; /* 18 */
            public bool IsSculptMirrored; /* 19 */

            public ushort PathBegin; /* 20 */
            public byte PathCurve; /* 22 */
            public ushort PathEnd; /* 23 */
            public sbyte PathRadiusOffset; /* 25 */
            public byte PathRevolutions; /* 26 */
            public byte PathScaleX; /* 27 */
            public byte PathScaleY; /* 28 */
            public byte PathShearX; /* 29 */
            public byte PathShearY; /* 30 */
            public sbyte PathSkew; /* 31 */
            public sbyte PathTaperX; /* 32 */
            public sbyte PathTaperY; /* 33 */
            public sbyte PathTwist; /* 34 */
            public sbyte PathTwistBegin; /* 35 */
            public ushort ProfileBegin; /* 36 */
            public byte ProfileCurve; /* 38 */
            public ushort ProfileEnd; /* 39 */
            public ushort ProfileHollow; /* 41 */

            public PrimitiveCode PCode; /* byte / 43 */

            public byte State; /* 44 */
            #endregion

            #region Serialization
            public byte[] Serialization
            {
                get
                {
                    var serialized = new byte[45];
                    SculptMap.ToBytes(serialized, 0);
                    serialized[16] = (byte)Type;
                    serialized[17] = (byte)SculptType;
                    serialized[18] = (byte)(IsSculptInverted ? 1 : 0);
                    serialized[19] = (byte)(IsSculptMirrored ? 1 : 0);
                    Buffer.BlockCopy(BitConverter.GetBytes(PathBegin), 0, serialized, 20, 2);
                    serialized[22] = PathCurve;
                    Buffer.BlockCopy(BitConverter.GetBytes(PathEnd), 0, serialized, 23, 2);
                    serialized[25] = (byte)PathRadiusOffset;
                    serialized[26] = PathRevolutions;
                    serialized[27] = PathScaleX;
                    serialized[28] = PathScaleY;
                    serialized[29] = PathShearX;
                    serialized[30] = PathShearY;
                    serialized[31] = (byte)PathSkew;
                    serialized[32] = (byte)PathTaperX;
                    serialized[33] = (byte)PathTaperY;
                    serialized[34] = (byte)PathTwist;
                    serialized[35] = (byte)PathTwistBegin;
                    Buffer.BlockCopy(BitConverter.GetBytes(ProfileBegin), 0, serialized, 36, 2);
                    serialized[38] = ProfileCurve;
                    Buffer.BlockCopy(BitConverter.GetBytes(ProfileEnd), 0, serialized, 39, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(ProfileHollow), 0, serialized, 41, 2);
                    serialized[43] = (byte)PCode;
                    serialized[44] = State;
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(serialized, 20, 2);
                        Array.Reverse(serialized, 23, 2);
                        Array.Reverse(serialized, 36, 2);
                        Array.Reverse(serialized, 39, 2);
                        Array.Reverse(serialized, 41, 2);
                    }

                    return serialized;
                }

                set
                {
                    if (value.Length != 45)
                    {
                        throw new ArgumentException("Array length must be 45.");
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(value, 20, 2);
                        Array.Reverse(value, 23, 2);
                        Array.Reverse(value, 36, 2);
                        Array.Reverse(value, 39, 2);
                        Array.Reverse(value, 41, 2);
                    }

                    SculptMap.FromBytes(value, 0);
                    SculptType = (PrimitiveSculptType)value[17];
                    if (SculptMap == UUID.Zero)
                    {
                        SculptType = PrimitiveSculptType.None;
                    }
                    IsSculptInverted = value[18] != 0;
                    IsSculptMirrored = value[19] != 0;
                    PathBegin = BitConverter.ToUInt16(value, 20);
                    PathCurve = value[22];
                    PathEnd = BitConverter.ToUInt16(value, 23);
                    PathRadiusOffset = (sbyte)value[25];
                    PathRevolutions = value[26];
                    PathScaleX = value[27];
                    PathScaleY = value[28];
                    PathShearX = value[29];
                    PathShearY = value[30];
                    PathSkew = (sbyte)value[31];
                    PathTaperX = (sbyte)value[32];
                    PathTaperY = (sbyte)value[33];
                    PathTwist = (sbyte)value[34];
                    PathTwistBegin = (sbyte)value[35];
                    ProfileBegin = BitConverter.ToUInt16(value, 36);
                    ProfileCurve = value[38];
                    ProfileEnd = BitConverter.ToUInt16(value, 39);
                    ProfileHollow = BitConverter.ToUInt16(value, 41);
                    PCode = (PrimitiveCode)value[43];
                    State = value[44];

                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(value, 20, 2);
                        Array.Reverse(value, 23, 2);
                        Array.Reverse(value, 36, 2);
                        Array.Reverse(value, 39, 2);
                        Array.Reverse(value, 41, 2);
                    }
                }
            }
            #endregion

            #region Properties
            public PrimitiveShapeType Type
            {
                get
                {
                    if (SculptType != PrimitiveSculptType.None)
                    {
                        return PrimitiveShapeType.Sculpt;
                    }

                    PrimitiveProfileShape profileShape = (PrimitiveProfileShape)(ProfileCurve & (byte)PrimitiveProfileShape.Mask);
                    PrimitiveExtrusion extrusion = (PrimitiveExtrusion)PathCurve;

                    switch (profileShape)
                    {
                        case PrimitiveProfileShape.Square:
                            if (extrusion == PrimitiveExtrusion.Curve1)
                            {
                                return PrimitiveShapeType.Tube;
                            }
                            break;

                        case PrimitiveProfileShape.Circle:
                            switch (extrusion)
                            {
                                case PrimitiveExtrusion.Straight:
                                case PrimitiveExtrusion.Default:
                                    return PrimitiveShapeType.Cylinder;

                                case PrimitiveExtrusion.Curve1:
                                    return PrimitiveShapeType.Torus;
                            }
                            break;

                        case PrimitiveProfileShape.HalfCircle:
                            if (extrusion == PrimitiveExtrusion.Curve1 || extrusion == PrimitiveExtrusion.Curve2)
                            {
                                return PrimitiveShapeType.Sphere;
                            }
                            break;

                        case PrimitiveProfileShape.EquilateralTriangle:
                            switch (extrusion)
                            {
                                case PrimitiveExtrusion.Straight:
                                case PrimitiveExtrusion.Default:
                                    return PrimitiveShapeType.Prism;

                                case PrimitiveExtrusion.Curve1:
                                    return PrimitiveShapeType.Ring;
                            }
                            break;
                    }

                    return PrimitiveShapeType.Box;
                }
            }

            public int NumberOfSides
            {
                get
                {
                    int ret = 0;
                    bool hasCut;
                    bool hasHollow;
                    bool hasDimple;
                    bool hasProfileCut;

                    var primType = Type;
                    hasCut = (primType == PrimitiveShapeType.Box ||
                        primType == PrimitiveShapeType.Cylinder ||
                        primType == PrimitiveShapeType.Prism) ?
                        (ProfileBegin > 0 || ProfileEnd > 0) :
                        (PathBegin > 0 || PathEnd > 0);

                    hasHollow = ProfileHollow > 0;
                    hasDimple = (ProfileBegin > 0) || (ProfileEnd > 0); // taken from llSetPrimitiveParms
                    hasProfileCut = hasDimple; // is it the same thing?

                    switch (primType)
                    {
                        case PrimitiveShapeType.Box:
                            ret = 6;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret++;
                            }
                            break;
                        case PrimitiveShapeType.Cylinder:
                            ret = 3;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret++;
                            }
                            break;
                        case PrimitiveShapeType.Prism:
                            ret = 5;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret++;
                            }
                            break;
                        case PrimitiveShapeType.Sphere:
                            ret = 1;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasDimple)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret++;
                            }
                            break;
                        case PrimitiveShapeType.Torus:
                            ret = 1;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasProfileCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret++;
                            }
                            break;
                        case PrimitiveShapeType.Tube:
                            ret = 4;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasProfileCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret++;
                            }
                            break;
                        case PrimitiveShapeType.Ring:
                            ret = 3;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasProfileCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret++;
                            }
                            break;
                        case PrimitiveShapeType.Sculpt:
                            // Special mesh handling
                            ret = (SculptType == PrimitiveSculptType.Mesh) ?
                                32 : // if it's a mesh then max 32 faces
                                1; // if it's a sculpt then max 1 face
                            break;
                        default:
                            break;
                    }

                    return ret;
                }
            }
            #endregion

            public void CopyFrom(PrimitiveShape shape)
            {
                SculptMap = shape.SculptMap;
                SculptType = shape.SculptType;
                IsSculptInverted = shape.IsSculptInverted;
                IsSculptMirrored = shape.IsSculptMirrored;

                PCode = shape.PCode;
                State = shape.State;

                PathBegin = shape.PathBegin;
                PathCurve = shape.PathCurve;
                PathEnd = shape.PathEnd;
                PathRadiusOffset = shape.PathRadiusOffset;
                PathRevolutions = shape.PathRevolutions;
                PathScaleX = shape.PathScaleX;
                PathScaleY = shape.PathScaleY;
                PathShearX = shape.PathShearX;
                PathShearY = shape.PathShearY;
                PathSkew = shape.PathSkew;
                PathTaperX = shape.PathTaperX;
                PathTaperY = shape.PathTaperY;
                PathTwist = shape.PathTwist;
                PathTwistBegin = shape.PathTwistBegin;
                ProfileBegin = shape.ProfileBegin;
                ProfileCurve = shape.ProfileCurve;
                ProfileEnd = shape.ProfileEnd;
                ProfileHollow = shape.ProfileHollow;
                ProfileEnd = shape.ProfileEnd;
            }

            public struct Decoded
            {
                #region Overall Params
                public PrimitiveShapeType ShapeType;
                public PrimitiveSculptType SculptType;
                public UUID SculptMap;
                public bool IsSculptInverted;
                public bool IsSculptMirrored;
                #endregion

                #region Profile Params
                public PrimitiveProfileShape ProfileShape;
                public PrimitiveProfileHollowShape HoleShape;
                /** <summary>value range 0f to 1f</summary> */
                public double ProfileBegin;
                /** <summary>value range 0f to 1f</summary> */
                public double ProfileEnd;
                /** <summary>value range 0f to 0.99f</summary> */
                public double ProfileHollow;
                public bool IsHollow;
                #endregion

                #region Path Params
                /** <summary>value range 0f to 1f</summary> */
                public double PathBegin;
                public PrimitiveExtrusion PathCurve;
                public bool IsOpen;
                /** <summary>value range 0f to 1f</summary> */
                public double PathEnd;
                /** <summary>value range 0f to 1f</summary> */
                public Vector3 PathScale;
                /** <summary>value range -1f to 1f</summary> */
                public Vector3 TopShear;
                /** <summary>value range -1f to 1f</summary> */
                public double TwistBegin;
                /** <summary>value range -1f to 1f</summary> */
                public double TwistEnd;
                public double RadiusOffset;
                /** <summary>value range -1f to 1f</summary> */
                public Vector3 Taper;
                /** <summary>value range 1f to 4f</summary> */
                public double Revolutions;
                /** <summary>value range -0.95f to 0.95f</summary> */
                public double Skew;
                #endregion
            }

            public bool IsSane
            {
                get
                {
                    PrimitiveProfileShape profileShape = ((PrimitiveProfileShape)ProfileCurve) & PrimitiveProfileShape.Mask;
                    PrimitiveProfileHollowShape holeShape = ((PrimitiveProfileHollowShape)ProfileCurve) & PrimitiveProfileHollowShape.Mask;
                    int path_type = PathCurve >> 4;
                    bool valid = true;
                    valid = valid && (profileShape <= PrimitiveProfileShape.HalfCircle);
                    valid = valid && (holeShape <= PrimitiveProfileHollowShape.Triangle);
                    valid = valid && (path_type >= 1 && path_type <= 8);
                    valid = valid && ((Type != PrimitiveShapeType.Box || Type != PrimitiveShapeType.Tube) &&
                        holeShape == PrimitiveProfileHollowShape.Square) ?
                        (ProfileHollow / 50000.0).IsInRange(0f, 0.7f) :
                        (ProfileHollow / 50000.0).IsInRange(0f, 0.99f);
                    valid = valid && ((ProfileBegin / 50000.0).IsInRange(0, 1) &&
                        (ProfileEnd / 50000.0).IsInRange(0, 1));
                    valid = valid && ProfileBegin + ProfileEnd < 50000;
                    valid = valid && (PathBegin / 50000.0).IsInRange(0, 1);
                    valid = valid && ((50000 - PathEnd) / 50000.0).IsInRange(0, 1);
                    valid = valid && PathBegin + PathEnd < 50000;
                    valid = valid && (PathScaleX / 100.0 - 1).IsInRange(-1, 1);
                    valid = valid && (PathScaleY / 100.0 - 1).IsInRange(-1, 1);
                    valid = valid && ((sbyte)PathShearX / 100.0).IsInRange(-0.5, 0.5);
                    valid = valid && ((sbyte)PathShearY / 100.0).IsInRange(-0.5, 0.5);
                    valid = valid && (PathTwistBegin / 100.0).IsInRange(-1, 1);
                    valid = valid && (PathTwist / 100.0).IsInRange(-1, 1);
                    //valid = valid && (PathRadiusOffset / 100.0); TODO: see LLVolumeParams.setRadiusOffset for this
                    valid = valid && (PathTaperX / 100.0).IsInRange(-1, 1);
                    valid = valid && (PathTaperY / 100.0).IsInRange(-1, 1);
                    valid = valid && (PathRevolutions / 100.0 + 1).IsInRange(1, 4);
                    valid = valid && (PathSkew / 100.0).IsInRange(-1, 1);
                    valid = valid && (SculptType & PrimitiveSculptType.TypeMask) <= PrimitiveSculptType.Mesh;
                    return valid;
                }
            }

            public Decoded DecodedParams
            {
                get
                {
                    var d = new Decoded
                    {
                        ShapeType = Type,
                        SculptType = SculptType,
                        SculptMap = SculptMap,
                        IsSculptInverted = IsSculptInverted,
                        IsSculptMirrored = IsSculptMirrored,

                        #region Profile Params
                        ProfileBegin = (ProfileBegin/ 50000.0).Clamp(0f, 1f),
                        ProfileEnd = 1 - (ProfileEnd / 50000.0).Clamp(0f, 1f),
                        IsOpen = ProfileBegin != 0 || ProfileEnd != 0,
                        ProfileShape = (PrimitiveProfileShape)(ProfileCurve & (byte)PrimitiveProfileShape.Mask),
                        HoleShape = (PrimitiveProfileHollowShape)(ProfileCurve & (byte)PrimitiveProfileHollowShape.Mask),
                        #endregion

                        PathCurve = (PrimitiveExtrusion)PathCurve
                    };

                    if(ProfileEnd == 50000)
                    {
                        d.ProfileEnd = 1;
                    }

                    #region Profile Params
                    d.ProfileHollow = ((Type != PrimitiveShapeType.Box || Type != PrimitiveShapeType.Tube) &&
                        d.HoleShape == PrimitiveProfileHollowShape.Square) ?
                        (ProfileHollow / 50000.0).Clamp(0f, 0.7f) :
                        (ProfileHollow / 50000.0).Clamp(0f, 0.99f);
                    d.IsHollow = ProfileHollow > 0;
                    #endregion

                    #region Path Rarams
                    d.PathBegin = (PathBegin / 50000.0).Clamp(0f, 1f);
                    d.PathEnd = ((50000 - PathEnd) / 50000.0).Clamp(0f, 1f);
                    if(PathEnd == 0)
                    {
                        d.PathEnd = 1;
                    }
                    d.PathScale = new Vector3(
                        (PathScaleX / 100.0 - 1).Clamp(-1, 1),
                        (PathScaleY / 100.0 - 1).Clamp(-1, 1),
                        0f);
                    d.TopShear = new Vector3(
                        ((sbyte)PathShearX / 100.0).Clamp(-0.5, 0.5),
                        ((sbyte)PathShearY / 100.0).Clamp(-0.5, 0.5),
                        0f);
                    d.TwistBegin = (PathTwistBegin / 100.0).Clamp(-1f, 1f);
                    d.TwistEnd = (PathTwist / 100.0).Clamp(-1f, 1f);
                    d.RadiusOffset = PathRadiusOffset / 100.0;
                    d.Taper = new Vector3(
                        (PathTaperX / 100.0).Clamp(-1f, 1f),
                        (PathTaperY / 100.0).Clamp(-1f, 1f),
                        0f);
                    d.Revolutions = (PathRevolutions / 100.0 + 1f).Clamp(1f, 4f);
                    d.Skew = (PathSkew / 100.0).Clamp(-1f, 1f);
                    #endregion

                    return d;
                }

                set
                {
                    SculptType = value.SculptType;
                    SculptMap = value.SculptMap;
                    IsSculptInverted = value.IsSculptInverted;
                    IsSculptMirrored = value.IsSculptMirrored;
                    PathCurve = (byte)value.PathCurve;
                    ProfileBegin = (ushort)(value.ProfileBegin * 50000).Clamp(0, 50000);
                    ProfileEnd = (ushort)((1 - value.ProfileEnd) * 50000).Clamp(0, 50000);
                    ProfileCurve = (byte)(((byte)value.ProfileShape) | ((byte)value.HoleShape));
                    if ((Type != PrimitiveShapeType.Box || Type != PrimitiveShapeType.Tube) &&
                        value.HoleShape == PrimitiveProfileHollowShape.Square)
                    {
                        ProfileHollow = (ushort)(value.ProfileHollow * 50000 / 0.7).Clamp(0, 50000);
                    }
                    else
                    {
                        ProfileHollow = (ushort)(value.ProfileHollow * 50000).Clamp(0, 50000);
                    }

                    PathBegin = (ushort)(value.PathBegin * 50000).Clamp(0, 1);
                    PathEnd = (ushort)(50000 - (ushort)(value.PathEnd * 50000).Clamp(0, 50000));
                    PathScaleX = (byte)(value.PathScale.X * 100 + 100).Clamp(0, 200);
                    PathScaleY = (byte)(value.PathScale.Y * 100 + 100).Clamp(0, 200);
                    PathTwistBegin = (sbyte)(value.TwistBegin * 100).Clamp(-100, 100);
                    PathTwist = (sbyte)(value.TwistEnd * 100).Clamp(-100, 100);
                    PathRadiusOffset = (sbyte)(value.RadiusOffset * 100).Clamp(-100, 100);
                    PathTaperX = (sbyte)(value.Taper.X * 100).Clamp(-100, 100);
                    PathTaperY = (sbyte)(value.Taper.Y * 100).Clamp(-100, 100);
                    PathRevolutions = (byte)((value.Revolutions.Clamp(1, 4) - 1) * 100);
                    PathSkew = (sbyte)(value.Skew * 100).Clamp(-100, 100);
                }
            }

            public void ToPrimitiveParams(AnArray paramList)
            {
                paramList.Add((int)Type);
                if (Type == PrimitiveShapeType.Sculpt)
                {
                    paramList.Add(SculptMap);
                    var sculptFlags = (int)SculptType;
                    if (IsSculptInverted)
                    {
                        sculptFlags |= 0x40;
                    }
                    if (IsSculptMirrored)
                    {
                        sculptFlags |= 0x80;
                    }
                    paramList.Add(sculptFlags);
                }
                else
                {
                    double topshearx = (sbyte)PathShearX / 100.0; // Fix negative values for PathShearX
                    double topsheary = (sbyte)PathShearY / 100.0; // and PathShearY.

                    switch (Type)
                    {
                        case PrimitiveShapeType.Box:
                        case PrimitiveShapeType.Cylinder:
                        case PrimitiveShapeType.Prism:
                            paramList.Add(ProfileCurve & 0xF0);
                            paramList.Add(new Vector3(ProfileBegin / 50000f, ProfileEnd / 50000f, 0));
                            paramList.Add(ProfileHollow / 50000f);
                            paramList.Add(new Vector3(PathTwistBegin / 100f, PathTwist / 100f, 0));
                            paramList.Add(new Vector3(1 - (PathScaleX / 100.0 - 1), 1 - (PathScaleY / 100f - 1), 0));
                            paramList.Add(new Vector3(topshearx, topsheary, 0));
                            break;

                        case PrimitiveShapeType.Sphere:
                            paramList.Add(ProfileCurve & 0xF0);
                            paramList.Add(new Vector3(PathBegin / 50000f, 1 - PathEnd / 50000f, 0));
                            paramList.Add(ProfileHollow / 50000f);
                            paramList.Add(new Vector3(PathTwistBegin / 100f, PathTwist / 100f, 0));
                            paramList.Add(new Vector3(ProfileBegin / 50000f, 1 - ProfileEnd / 50000f, 0));
                            break;

                        case PrimitiveShapeType.Torus:
                        case PrimitiveShapeType.Tube:
                        case PrimitiveShapeType.Ring:
                            paramList.Add(ProfileCurve & 0xf0);
                            paramList.Add(new Vector3(PathBegin / 50000f, PathEnd / 50000f, 0));
                            paramList.Add(ProfileHollow / 50000f);
                            paramList.Add(new Vector3(PathTwistBegin / 100f, PathTwist / 100f, 0));
                            paramList.Add(new Vector3(1 - (PathScaleX / 100f - 1), 1 - (PathScaleY / 100f - 1), 0));
                            paramList.Add(new Vector3(topshearx, topsheary, 0));
                            paramList.Add(new Vector3(ProfileBegin / 50000f, 1 - ProfileEnd / 50000f, 0));
                            paramList.Add(new Vector3(PathTaperX / 100f, PathTaperY / 100f, 0));
                            paramList.Add(Math.Round(PathRevolutions * 0.015d, 2, MidpointRounding.AwayFromZero) + 1d);
                            paramList.Add(PathRadiusOffset / 100f);
                            paramList.Add(PathSkew / 100f);
                            break;
                        default:
                            break;
                    }
                }
            }

            public static PrimitiveShape FromPrimitiveParams(AnArray.MarkEnumerator enumerator)
            {
                var shape = new PrimitiveShape();
                var shapeType = (PrimitiveShapeType)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");

                if (shapeType == PrimitiveShapeType.Sculpt)
                {
                    shape.SculptMap = ParamsHelper.GetKey(enumerator, "PRIM_TYPE");
                    int sculptFlags = ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
                    shape.SculptType = (PrimitiveSculptType)(sculptFlags & 0x0F);
                    shape.IsSculptInverted = (sculptFlags & 0x40) != 0;
                    shape.IsSculptMirrored = (sculptFlags & 0x80) != 0;
                }
                else
                {
                    var holeShape = (PrimitiveHoleShape)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
                    if (holeShape != PrimitiveHoleShape.Circle &&
                        holeShape != PrimitiveHoleShape.Default &&
                        holeShape != PrimitiveHoleShape.Square &&
                        holeShape != PrimitiveHoleShape.Triangle)
                    {
                        holeShape = PrimitiveHoleShape.Default;
                    }
                    var profileShape = PrimitiveProfileShape.Circle;
                    var extrusion = PrimitiveExtrusion.Straight;
                    switch (shapeType)
                    {
                        case PrimitiveShapeType.Box:
                            profileShape = PrimitiveProfileShape.Square;
                            extrusion = PrimitiveExtrusion.Straight;
                            break;

                        case PrimitiveShapeType.Cylinder:
                            profileShape = PrimitiveProfileShape.Circle;
                            extrusion = PrimitiveExtrusion.Straight;
                            break;

                        case PrimitiveShapeType.Prism:
                            profileShape = PrimitiveProfileShape.EquilateralTriangle;
                            extrusion = PrimitiveExtrusion.Straight;
                            break;

                        case PrimitiveShapeType.Sphere:
                            profileShape = PrimitiveProfileShape.HalfCircle;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Torus:
                            profileShape = PrimitiveProfileShape.Circle;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Tube:
                            profileShape = PrimitiveProfileShape.Square;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Ring:
                            profileShape = PrimitiveProfileShape.EquilateralTriangle;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Sculpt:
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        default:
                            break;
                    }

                    shape.PathCurve = (byte)extrusion;
                    shape.ProfileCurve = (byte)holeShape;
                    shape.ProfileCurve |= (byte)profileShape;
                    Vector3 cut = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                    double hollow = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                    Vector3 twist = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");

                    cut.X = cut.X.Clamp(0, 1);
                    cut.Y = cut.Y.Clamp(0, 1);
                    if (cut.Y - cut.X < 0.05f)
                    {
                        cut.Y -= 0.05f;
                        if (cut.X < 0.0f)
                        {
                            cut.X = 0.0f;
                            cut.Y = 0.05f;
                        }
                    }
                    shape.ProfileBegin = (ushort)(50000 * cut.X);
                    shape.ProfileEnd = (ushort)(50000 * (1 - cut.Y));

                    // If the prim is a Cylinder, Prism, Sphere, Torus or Ring (or not a
                    // Box or Tube) and the hole shape is a square, hollow is limited to
                    // a max of 70%. The viewer performs its own check on this value but
                    // we need to do it here also so llGetPrimitiveParams can have access
                    // to the correct value.
                    hollow = (profileShape != PrimitiveProfileShape.Square &&
                        holeShape == PrimitiveHoleShape.Square) ?
                        hollow.Clamp(0f, 0.70f) :
                    // Otherwise, hollow is limited to 95%.
                        hollow.Clamp(0f, 0.95f);
                    shape.ProfileHollow = (ushort)(50000 * hollow);
                    twist.X = twist.X.Clamp(-1f, 1f);
                    twist.Y = twist.Y.Clamp(-1f, 1f);

                    double tempFloat = 100.0d * twist.X;
                    shape.PathTwistBegin = (sbyte)tempFloat;
                    tempFloat = 100.0d * twist.Y;
                    shape.PathTwist = (sbyte)tempFloat;

                    Vector3 topSize;
                    Vector3 topShear;
                    Vector3 holeSize;
                    Vector3 dimple;

                    switch (shapeType)
                    {
                        case PrimitiveShapeType.Box:
                        case PrimitiveShapeType.Cylinder:
                        case PrimitiveShapeType.Prism:
                            topSize = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            topShear = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");

                            topSize.X = topSize.X.Clamp(0f, 2f);
                            topSize.Y = topSize.Y.Clamp(0f, 2f);
                            tempFloat = (float)(100.0d * (2.0d - topSize.X));
                            shape.PathScaleX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * (2.0d - topSize.Y));
                            shape.PathScaleY = (byte)tempFloat;
                            topShear.X = topShear.X.Clamp(-0.5f, 0.5f);
                            topShear.Y = topShear.Y.Clamp(-0.5f, 0.5f);
                            tempFloat = (float)(100.0d * topShear.X);
                            shape.PathShearX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * topShear.Y);
                            shape.PathShearY = (byte)tempFloat;
                            break;

                        case PrimitiveShapeType.Sphere:
                            dimple = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");

                            // profile/path swapped for a sphere
                            shape.PathBegin = shape.ProfileBegin;
                            shape.PathEnd = shape.ProfileEnd;

                            shape.PathScaleX = 100;
                            shape.PathScaleY = 100;

                            dimple.X = dimple.X.Clamp(0f, 1f);
                            dimple.Y = dimple.Y.Clamp(0f, 1f);
                            if (dimple.Y - cut.X < 0.05f)
                            {
                                dimple.X = cut.Y - 0.05f;
                            }
                            shape.ProfileBegin = (ushort)(50000 * dimple.X);
                            shape.ProfileEnd = (ushort)(50000 * (1 - dimple.Y));
                            break;

                        case PrimitiveShapeType.Torus:
                        case PrimitiveShapeType.Tube:
                        case PrimitiveShapeType.Ring:
                            holeSize = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            topShear = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            Vector3 advancedCut = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            Vector3 taper = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            double revolutions = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                            double radiusOffset = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                            double skew = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");

                            // profile/path swapped for a torrus, tube, ring
                            shape.PathBegin = shape.ProfileBegin;
                            shape.PathEnd = shape.ProfileEnd;

                            holeSize.X = holeSize.X.Clamp(0.05f, 1f);
                            holeSize.Y = holeSize.Y.Clamp(0.05f, 0.5f);
                            tempFloat = (float)(100.0d * (2.0d - holeSize.X));
                            shape.PathScaleX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * (2.0d - holeSize.Y));
                            shape.PathScaleY = (byte)tempFloat;
                            topShear.X = topShear.X.Clamp(-0.5f, 0.5f);
                            topShear.Y = topShear.Y.Clamp(-0.5f, 0.5f);
                            tempFloat = (float)(100.0d * topShear.X);
                            shape.PathShearX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * topShear.Y);
                            shape.PathShearY = (byte)tempFloat;
                            advancedCut.X = advancedCut.X.Clamp(0f, 1f);
                            advancedCut.Y = advancedCut.Y.Clamp(0f, 1f);
                            if (advancedCut.Y - advancedCut.X < 0.05f)
                            {
                                advancedCut.X = advancedCut.Y - 0.05f;
                                if (advancedCut.X < 0.0f)
                                {
                                    advancedCut.X = 0.0f;
                                    advancedCut.Y = 0.05f;
                                }
                            }
                            shape.ProfileBegin = (ushort)(50000 * advancedCut.X);
                            shape.ProfileEnd = (ushort)(50000 * (1 - advancedCut.Y));
                            taper.X = taper.X.Clamp(-1f, 1f);
                            taper.Y = taper.Y.Clamp(-1f, 1f);
                            tempFloat = (float)(100.0d * taper.X);
                            shape.PathTaperX = (sbyte)tempFloat;
                            tempFloat = (float)(100.0d * taper.Y);
                            shape.PathTaperY = (sbyte)tempFloat;
                            revolutions = revolutions.Clamp(1f, 4f);
                            tempFloat = 66.66667f * (revolutions - 1.0f);
                            shape.PathRevolutions = (byte)tempFloat;
                            // limits on radiusoffset depend on revolutions and hole size (how?) seems like the maximum range is 0 to 1
                            radiusOffset = radiusOffset.Clamp(0f, 1f);
                            tempFloat = 100.0f * radiusOffset;
                            shape.PathRadiusOffset = (sbyte)tempFloat;
                            skew = skew.Clamp(-0.95f, 0.95f);
                            tempFloat = 100.0f * skew;
                            shape.PathSkew = (sbyte)tempFloat;
                            break;

                        default:
                            throw new ArgumentException(String.Format("Invalid primitive type {0}", shape.Type));
                    }
                }

                return shape;
            }

            public bool Equals(PrimitiveShape other)
            {
                byte[] a = Serialization;
                byte[] b = other.Serialization;
                if(a.Length == b.Length)
                {
                    for(int i = 0; i < a.Length; ++i)
                    {
                        if(a[i] != b[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }

            public override bool Equals(object o)
            {
                var s = o as PrimitiveShape;
                if(s == null)
                {
                    return false;
                }
                return Equals(s);
            }

            public override int GetHashCode()
            {
                int h = 0;
                byte[] d = Serialization;
                foreach(byte b in d)
                {
                    h ^= b.GetHashCode();
                }
                return h;
            }
        }

        private readonly PrimitiveShape m_Shape = new PrimitiveShape();

        public PrimitiveShape Shape
        {
            get
            {
                var res = new PrimitiveShape();
                lock (m_Shape)
                {
                    res.CopyFrom(m_Shape);
                }
                return res;
            }
            set
            {
                bool sculptChanged = false;
                lock (m_Shape)
                {
                    if (m_Shape.SculptMap != value.SculptMap || m_Shape.SculptType != value.SculptType)
                    {
                        sculptChanged = true;
                    }
                    m_Shape.CopyFrom(value);
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetPrimitiveShape(value);
                    }
                }

                if (sculptChanged)
                {
                    UpdateExtraParams();
                }
                IncrementPhysicsShapeUpdateSerial();
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        #region Primitive Methods
        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList) => GetPrimitiveParams(enumerator, paramList, null);

        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList, CultureInfo cultureInfo)
        {
            ObjectPartLocalizedInfo localization = GetLocalization(cultureInfo);
            PrimitiveParamsType paramtype = ParamsHelper.GetPrimParamType(enumerator);
            switch (paramtype)
            {
                case PrimitiveParamsType.Name:
                    paramList.Add(localization.Name);
                    break;

                case PrimitiveParamsType.Desc:
                    paramList.Add(localization.Description);
                    break;

                case PrimitiveParamsType.Type:
                    Shape.ToPrimitiveParams(paramList);
                    break;

                case PrimitiveParamsType.Slice:
                    paramList.Add(Slice);
                    break;

                case PrimitiveParamsType.PhysicsShapeType:
                    paramList.Add((int)PhysicsShapeType);
                    break;

                case PrimitiveParamsType.Material:
                    paramList.Add((int)Material);
                    break;

                case PrimitiveParamsType.Position:
                    paramList.Add(Position);
                    break;

                case PrimitiveParamsType.PosLocal:
                    paramList.Add(LocalPosition);
                    break;

                case PrimitiveParamsType.Rotation:
                    paramList.Add(Rotation);
                    break;

                case PrimitiveParamsType.RotLocal:
                    paramList.Add(LocalRotation);
                    break;

                case PrimitiveParamsType.Size:
                    paramList.Add(Size);
                    break;

                case PrimitiveParamsType.AlphaMode:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_ALPHAMODE");
                    break;

                case PrimitiveParamsType.Normal:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_NORMAL");
                    break;

                case PrimitiveParamsType.Specular:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_SPECULAR");
                    break;

                case PrimitiveParamsType.Texture:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_TEXTURE");
                    break;

                case PrimitiveParamsType.Text:
                    {
                        TextParam text = localization.Text;
                        paramList.Add(text.Text);
                        paramList.Add(text.TextColor.AsVector3);
                        paramList.Add(text.TextColor.A);
                    }
                    break;

                case PrimitiveParamsType.Color:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_COLOR");
                    break;

                case PrimitiveParamsType.BumpShiny:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_BUMP_SHINY");
                    break;

                case PrimitiveParamsType.PointLight:
                    {
                        PointLightParam p = PointLight;
                        paramList.Add(p.IsLight);
                        paramList.Add(p.LightColor.AsVector3);
                        paramList.Add(p.Intensity);
                        paramList.Add(p.Radius);
                        paramList.Add(p.Falloff);
                    }
                    break;

                case PrimitiveParamsType.FullBright:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_FULLBRIGHT");
                    break;

                case PrimitiveParamsType.Flexible:
                    {
                        var p = Flexible;
                        paramList.Add(p.IsFlexible);
                        paramList.Add(p.Softness);
                        paramList.Add(p.Gravity);
                        paramList.Add(p.Friction);
                        paramList.Add(p.Wind);
                        paramList.Add(p.Tension);
                        paramList.Add(p.Force);
                    }
                    break;

                case PrimitiveParamsType.TexGen:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_TEXGEN");
                    break;

                case PrimitiveParamsType.Glow:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_GLOW");
                    break;

                case PrimitiveParamsType.Omega:
                    {
                        var p = Omega;
                        paramList.Add(p.Axis);
                        paramList.Add(p.Spinrate);
                        paramList.Add(p.Gain);
                    }
                    break;

                case PrimitiveParamsType.Alpha:
                    localization.GetTexPrimitiveParams(enumerator, paramtype, paramList, "PRIM_ALPHA");
                    break;

                case PrimitiveParamsType.AllowUnsit:
                    paramList.Add(AllowUnsit ? 1 : 0);
                    break;

                case PrimitiveParamsType.ScriptedSitOnly:
                    paramList.Add(IsScriptedSitOnly ? 1 : 0);
                    break;

                case PrimitiveParamsType.SitTarget:
                    paramList.Add(IsSitTargetActive);
                    paramList.Add(SitTargetOffset);
                    paramList.Add(SitTargetOrientation);
                    break;

                case PrimitiveParamsType.UnSitTarget:
                    paramList.Add(IsUnSitTargetActive);
                    paramList.Add(UnSitTargetOffset);
                    paramList.Add(UnSitTargetOrientation);
                    break;

                case PrimitiveParamsType.SitAnimation:
                    paramList.Add(SitAnimation);
                    break;

                case PrimitiveParamsType.Projector:
                    {
                        var param = localization.Projection;
                        paramList.Add(param.IsProjecting ? 1 : 0);
                        paramList.Add(param.ProjectionTextureID);
                        paramList.Add(param.ProjectionFOV);
                        paramList.Add(param.ProjectionFocus);
                        paramList.Add(param.ProjectionAmbience);
                    }
                    break;

                case PrimitiveParamsType.ProjectorEnabled:
                    {
                        var param = localization.Projection;
                        paramList.Add(param.IsProjecting ? 1 : 0);
                    }
                    break;

                case PrimitiveParamsType.ProjectorTexture:
                    {
                        var param = localization.Projection;
                        paramList.Add(param.ProjectionTextureID);
                    }
                    break;

                case PrimitiveParamsType.ProjectorFov:
                    {
                        var param = localization.Projection;
                        paramList.Add(param.ProjectionFOV);
                    }
                    break;

                case PrimitiveParamsType.ProjectorFocus:
                    {
                        var param = localization.Projection;
                        paramList.Add(param.ProjectionFocus);
                    }
                    break;

                case PrimitiveParamsType.ProjectorAmbience:
                    {
                        var param = localization.Projection;
                        paramList.Add(param.ProjectionAmbience);
                    }
                    break;

                default:
                    throw new LocalizedScriptErrorException(this, "PRIMInvalidParameterType0", "Invalid primitive parameter type {0}", enumerator.Current.AsUInt);
            }
        }

        public const int ALL_SIDES = -1;

        public int NumberOfSides => Shape.NumberOfSides;

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator) => SetPrimitiveParams(enumerator, null);

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator, string culturename)
        {
            ObjectPartLocalizedInfo localization = GetOrCreateLocalization(culturename);
            UpdateChangedFlags flags = 0;
            bool isUpdated = false;
            PrimitiveParamsType paramtype = ParamsHelper.GetPrimParamType(enumerator);
            switch (paramtype)
            {
                case PrimitiveParamsType.RemoveLanguage:
                    RemoveLocalization(ParamsHelper.GetString(enumerator, "PRIM_REMOVE_LANGUAGE"));
                    break;

                case PrimitiveParamsType.RemoveAllLanguages:
                    RemoveAllLocalizations();
                    break;

                case PrimitiveParamsType.Name:
                    localization.Name = ParamsHelper.GetString(enumerator, "PRIM_NAME");
                    break;

                case PrimitiveParamsType.Desc:
                    localization.Description = ParamsHelper.GetString(enumerator, "PRIM_DESC");
                    break;

                case PrimitiveParamsType.Type:
                    Shape = PrimitiveShape.FromPrimitiveParams(enumerator);
                    break;

                case PrimitiveParamsType.Slice:
                    Slice = ParamsHelper.GetVector(enumerator, "PRIM_SIZE");
                    break;

                case PrimitiveParamsType.PhysicsShapeType:
                    PhysicsShapeType = (PrimitivePhysicsShapeType)ParamsHelper.GetInteger(enumerator, "PRIM_PHYSICS_SHAPE_TYPE");
                    break;

                case PrimitiveParamsType.Material:
                    Material = (PrimitiveMaterial)ParamsHelper.GetInteger(enumerator, "PRIM_MATERIAL");
                    break;

                case PrimitiveParamsType.Position:
                    Position = ParamsHelper.GetVector(enumerator, "PRIM_POSITION");
                    break;

                case PrimitiveParamsType.PosLocal:
                    LocalPosition = ParamsHelper.GetVector(enumerator, "PRIM_POS_LOCAL");
                    break;

                case PrimitiveParamsType.Rotation:
                    Rotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROTATION").Normalize();
                    break;

                case PrimitiveParamsType.RotLocal:
                    LocalRotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROT_LOCAL").Normalize();
                    break;

                case PrimitiveParamsType.Size:
                    Size = ParamsHelper.GetVector(enumerator, "PRIM_SIZE");
                    break;

                case PrimitiveParamsType.AlphaMode:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_ALPHAMODE");
                    break;

                case PrimitiveParamsType.Normal:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_NORMAL");
                    break;

                case PrimitiveParamsType.Specular:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_SPECULAR");
                    break;

                case PrimitiveParamsType.Texture:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_TEXTURE");
                    break;

                case PrimitiveParamsType.Text:
                    localization.Text = new TextParam
                    {
                        Text = ParamsHelper.GetString(enumerator, "PRIM_TEXT"),
                        TextColor = new ColorAlpha(
                            ParamsHelper.GetVector(enumerator, "PRIM_TEXT"),
                            ParamsHelper.GetDouble(enumerator, "PRIM_TEXT"))
                    };
                    break;

                case PrimitiveParamsType.Color:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_COLOR");
                    break;

                case PrimitiveParamsType.Alpha:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_ALPHA");
                    break;

                case PrimitiveParamsType.BumpShiny:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_BUMP_SHINY");
                    break;

                case PrimitiveParamsType.PointLight:
                    PointLight = new PointLightParam
                    {
                        IsLight = ParamsHelper.GetBoolean(enumerator, "PRIM_POINT_LIGHT"),
                        LightColor = new Color(ParamsHelper.GetVector(enumerator, "PRIM_POINT_LIGHT")),
                        Intensity = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT"),
                        Radius = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT"),
                        Falloff = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT")
                    };
                    break;

                case PrimitiveParamsType.FullBright:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_FULLBRIGHT");
                    break;

                case PrimitiveParamsType.Flexible:
                    Flexible = new FlexibleParam
                    {
                        IsFlexible = ParamsHelper.GetBoolean(enumerator, "PRIM_FLEXIBLE"),
                        Softness = ParamsHelper.GetInteger(enumerator, "PRIM_FLEXIBLE"),
                        Gravity = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE"),
                        Friction = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE"),
                        Wind = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE"),
                        Force = ParamsHelper.GetVector(enumerator, "PRIM_FLEXIBLE")
                    };
                    break;

                case PrimitiveParamsType.TexGen:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_TEXGEN");
                    break;

                case PrimitiveParamsType.Glow:
                    localization.SetTexPrimitiveParams(paramtype, enumerator, ref flags, ref isUpdated, "PRIM_GLOW");
                    break;

                case PrimitiveParamsType.Omega:
                    Omega = new OmegaParam
                    {
                        Axis = ParamsHelper.GetVector(enumerator, "PRIM_OMEGA"),
                        Spinrate = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA"),
                        Gain = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA")
                    };
                    break;

                case PrimitiveParamsType.AllowUnsit:
                    AllowUnsit = ParamsHelper.GetBoolean(enumerator, "PRIM_ALLOW_UNSIT");
                    break;

                case PrimitiveParamsType.ScriptedSitOnly:
                    IsScriptedSitOnly = ParamsHelper.GetBoolean(enumerator, "PRIM_SCRIPTED_SIT_ONLY");
                    break;

                case PrimitiveParamsType.SitTarget:
                    {
                        bool sitenabled = ParamsHelper.GetBoolean(enumerator, "PRIM_SIT_TARGET");
                        Vector3 offset = ParamsHelper.GetVector(enumerator, "PRIM_SIT_TARGET");
                        Quaternion q = ParamsHelper.GetRotation(enumerator, "PRIM_SIT_TARGET");
                        if(sitenabled)
                        {
                            IsSitTargetActive = true;
                            SitTargetOffset = offset;
                            SitTargetOrientation = q;
                        }
                        else
                        {
                            IsSitTargetActive = false;
                            SitTargetOffset = Vector3.Zero;
                            SitTargetOrientation = Quaternion.Identity;
                        }
                    }
                    break;

                case PrimitiveParamsType.UnSitTarget:
                    {
                        bool unsitenabled = ParamsHelper.GetBoolean(enumerator, "PRIM_UNSIT_TARGET");
                        Vector3 offset = ParamsHelper.GetVector(enumerator, "PRIM_UNSIT_TARGET");
                        Quaternion q = ParamsHelper.GetRotation(enumerator, "PRIM_UNSIT_TARGET");
                        if (unsitenabled)
                        {
                            IsUnSitTargetActive = true;
                            UnSitTargetOffset = offset;
                            UnSitTargetOrientation = q;
                        }
                        else
                        {
                            IsUnSitTargetActive = false;
                            UnSitTargetOffset = Vector3.Zero;
                            UnSitTargetOrientation = Quaternion.Identity;
                        }
                    }
                    break;

                case PrimitiveParamsType.Projector:
                    localization.Projection = new ProjectionParam
                    {
                        IsProjecting = ParamsHelper.GetBoolean(enumerator, "PRIM_PROJECTOR"),
                        ProjectionTextureID = GetTextureParam(enumerator, "PRIM_PROJECTOR"),
                        ProjectionFOV = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR"),
                        ProjectionFocus = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR"),
                        ProjectionAmbience = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR")
                    };
                    break;

                case PrimitiveParamsType.ProjectorEnabled:
                    {
                        var param = localization.Projection;
                        param.IsProjecting = ParamsHelper.GetBoolean(enumerator, "PRIM_PROJECTOR_ENABLED");
                        localization.Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorTexture:
                    {
                        var param = localization.Projection;
                        param.ProjectionTextureID = GetTextureParam(enumerator, "PRIM_PROJECTOR_TEXTURE");
                        localization.Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorFov:
                    {
                        var param = localization.Projection;
                        param.ProjectionFOV = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR_FOV");
                        localization.Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorFocus:
                    {
                        ProjectionParam param = localization.Projection;
                        param.ProjectionFocus = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR_FOCUS");
                        localization.Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorAmbience:
                    {
                        var param = localization.Projection;
                        param.ProjectionAmbience = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTION_AMBIENCE");
                        localization.Projection = param;
                    }
                    break;

                case PrimitiveParamsType.SitAnimation:
                    SitAnimation = ParamsHelper.GetString(enumerator, "PRIM_SIT_ANIMATION");
                    break;

                default:
                    throw new LocalizedScriptErrorException(this, "PRIMInvalidParameterType0", "Invalid primitive parameter type {0}", enumerator.Current.AsUInt);
            }

            if(isUpdated)
            {
                TriggerOnUpdate(flags);
            }
        }
        #endregion

        #region TextureEntryFace functions
        internal string GetTextureInventoryItem(UUID assetID)
        {
            if (assetID != UUID.Zero)
            {
                foreach (var item in Inventory.Values)
                {
                    if (item.AssetType == AssetType.Texture && item.AssetID == assetID)
                    {
                        return item.Name;
                    }
                }
            }
            return assetID.ToString();
        }

        internal UUID GetTextureParam(IEnumerator<IValue> enumerator, string paraName)
        {
            var texture = ParamsHelper.GetString(enumerator, paraName);
            UUID uuid;
            ObjectPartInventoryItem texitem;
            if(UUID.TryParse(texture, out uuid))
            {
                return uuid;
            }
            else if(Inventory.TryGetValue(texture, out texitem) &&
                texitem.AssetType == AssetType.Texture)
            {
                return texitem.AssetID;
            }
            throw new ArgumentException("texture does not name either a inventory item or a uuid");
        }
        #endregion
    }
}
