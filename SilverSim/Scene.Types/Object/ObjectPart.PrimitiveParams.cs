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

using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

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
            public PrimitiveShapeType Type; /* byte / 16 */

            public UUID SculptMap = UUID.Zero; /* 0 */
            public PrimitiveSculptType SculptType = PrimitiveSculptType.Sphere; /* byte / 17 */
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
                    if(!BitConverter.IsLittleEndian)
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
                    if(value.Length != 45)
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
                    Type = (PrimitiveShapeType)value[16];
                    SculptType = (PrimitiveSculptType)value[17];
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
                Type = shape.Type;
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

            /** <summary>divides by 50000</summary> */
            private readonly double CutQuanta = 0.00002f;
            /** <summary>divides by 100</summary> */
            private readonly double ScaleQuanta = 0.01f;
            /** <summary>divides by 100</summary> */
            private const double ShearQuanta = 0.01f;
            /** <summary>divides by 100</summary> */
            private const double TaperQuanta = 0.01f;
            /** <summary>0.015f</summary> */
            private const double RevQuanta = 0.015f;
            /** <summary>divides by 50000</summary> */
            private const double HollowQuanta = 0.00002f;

            public Decoded DecodedParams
            {
                get
                {
                    var d = new Decoded()
                    {
                        ShapeType = Type,
                        SculptType = SculptType,
                        SculptMap = SculptMap,
                        IsSculptInverted = IsSculptInverted,
                        IsSculptMirrored = IsSculptMirrored,

                        #region Profile Params
                        ProfileBegin = (ProfileBegin * CutQuanta).Clamp(0f, 1f),
                        ProfileEnd = (ProfileEnd * CutQuanta).Clamp(0f, 1f),
                        IsOpen = ProfileBegin != 0 || ProfileEnd != 50000,
                        ProfileShape = (PrimitiveProfileShape)(ProfileCurve & (byte)PrimitiveProfileShape.Mask),
                        HoleShape = (PrimitiveProfileHollowShape)(ProfileCurve & (byte)PrimitiveProfileHollowShape.Mask)
                        #endregion
                    };

                    #region Profile Params
                    d.ProfileHollow = ((Type != PrimitiveShapeType.Box || Type != PrimitiveShapeType.Tube) &&
                        d.HoleShape == PrimitiveProfileHollowShape.Square) ?
                        (ProfileHollow * HollowQuanta).Clamp(0f, 0.7f) :
                        (ProfileHollow * HollowQuanta).Clamp(0f, 0.99f);
                    d.IsHollow = ProfileHollow > 0;
                    #endregion

                    #region Path Rarams
                    d.PathBegin = (PathBegin * CutQuanta).Clamp(0f, 1f);
                    d.PathEnd = ((50000 - PathEnd) * CutQuanta).Clamp(0f, 1f);
                    d.PathScale = new Vector3(
                        ((200 - PathScaleX) * ScaleQuanta).Clamp(0f, 1f),
                        ((200 - PathScaleY) * ScaleQuanta).Clamp(0f, 1f),
                        0f);
                    d.TopShear = new Vector3(
                        (PathShearX * ScaleQuanta - 0.5).Clamp(-0.5, 0.5),
                        (PathShearY * ScaleQuanta - 0.5).Clamp(-0.5, 0.5),
                        0f);
                    d.TwistBegin = (PathTwistBegin * ScaleQuanta).Clamp(-1f, 1f);
                    d.TwistEnd = (PathTwist * ScaleQuanta).Clamp(-1f, 1f);
                    d.RadiusOffset = PathRadiusOffset * ScaleQuanta;
                    d.Taper = new Vector3(
                        (PathTaperX * TaperQuanta).Clamp(-1f, 1f),
                        (PathTaperY * TaperQuanta).Clamp(-1f, 1f),
                        0f);
                    d.Revolutions = (PathRevolutions * RevQuanta + 1f).Clamp(1f, 4f);
                    d.Skew = (PathSkew * ScaleQuanta).Clamp(-1f, 1f);
                    #endregion

                    return d;
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
                var shape = new PrimitiveShape()
                {
                    Type = (PrimitiveShapeType)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE")
                };
                if (shape.Type == PrimitiveShapeType.Sculpt)
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
                    switch (shape.Type)
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

                    switch (shape.Type)
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
                }
                lock(m_UpdateDataLock)
                {
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathBegin] = (byte)(value.PathBegin % 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathBegin + 1] = (byte)(value.PathBegin / 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathEnd] = (byte)(value.PathEnd % 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathEnd + 1] = (byte)(value.PathEnd / 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathCurve] = value.PathCurve;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathRadiusOffset] = (byte)value.PathRadiusOffset;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathRevolutions] = value.PathRevolutions;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathScaleX] = value.PathScaleX;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathScaleY] = value.PathScaleY;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathShearX] = value.PathShearX;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathShearY] = value.PathShearY;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathSkew] = (byte)value.PathSkew;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTaperX] = (byte)value.PathTaperX;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTaperY] = (byte)value.PathTaperY;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTwist] = (byte)value.PathTwist;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTwistBegin] = (byte)value.PathTwistBegin;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PCode] = (byte)value.PCode;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin] = (byte)(value.ProfileBegin % 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin + 1] = (byte)(value.ProfileBegin / 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileCurve] = value.ProfileCurve;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd] = (byte)(value.ProfileEnd % 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd + 1] = (byte)(value.ProfileEnd / 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow] = (byte)(value.ProfileHollow % 256);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow + 1] = (byte)(value.ProfileHollow / 256);

                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathBegin] = (byte)(value.PathBegin % 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathBegin + 1] = (byte)(value.PathBegin / 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathEnd] = (byte)(value.PathEnd % 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathEnd + 1] = (byte)(value.PathEnd / 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathCurve] = value.PathCurve;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathRadiusOffset] = (byte)value.PathRadiusOffset;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathRevolutions] = value.PathRevolutions;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathScaleX] = value.PathScaleX;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathScaleY] = value.PathScaleY;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathShearX] = value.PathShearX;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathShearY] = value.PathShearY;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathSkew] = (byte)value.PathSkew;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTaperX] = (byte)value.PathTaperX;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTaperY] = (byte)value.PathTaperY;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTwist] = (byte)value.PathTwist;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTwistBegin] = (byte)value.PathTwistBegin;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin] = (byte)(value.ProfileBegin % 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin + 1] = (byte)(value.ProfileBegin / 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileCurve] = value.ProfileCurve;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd] = (byte)(value.ProfileEnd % 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd + 1] = (byte)(value.ProfileEnd / 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow] = (byte)(value.ProfileHollow % 256);
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow + 1] = (byte)(value.ProfileHollow / 256);

                    if (value.SculptType == PrimitiveSculptType.Mesh)
                    {
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin] = (byte)(12500 % 256);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin + 1] = (byte)(12500 / 256);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd] = 0;
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd + 1] = 0;
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow] = (byte)(27500 % 256);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow + 1] = (byte)(27500 / 256);

                        m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin] = (byte)(12500 % 256);
                        m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin + 1] = (byte)(12500 / 256);
                        m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd] = 0;
                        m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd + 1] = 0;
                        m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow] = (byte)(27500 % 256);
                        m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow + 1] = (byte)(27500 / 256);
                    }
                }
                if (sculptChanged)
                {
                    UpdateExtraParams();
                }
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        #region Primitive Methods
        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList)
        {
            switch (ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    paramList.Add(Name);
                    break;

                case PrimitiveParamsType.Desc:
                    paramList.Add(Description);
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
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_ALPHAMODE")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.AlphaMode, paramList);
                        }
                    });
                    break;

                case PrimitiveParamsType.Normal:
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_NORMAL")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Normal, paramList);
                        }
                    });
                    break;

                case PrimitiveParamsType.Specular:
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Specular, paramList);
                        }
                    });
                    break;

                case PrimitiveParamsType.Texture:
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    });
                    break;

                case PrimitiveParamsType.Text:
                    {
                        TextParam text = Text;
                        paramList.Add(text.Text);
                        paramList.Add(text.TextColor.AsVector3);
                        paramList.Add(text.TextColor.A);
                    }
                    break;

                case PrimitiveParamsType.Color:
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_COLOR")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Color, paramList);
                        }
                    });
                    break;

                case PrimitiveParamsType.BumpShiny:
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.BumpShiny, paramList);
                        }
                    });
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
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    });
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
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    });
                    break;

                case PrimitiveParamsType.Glow:
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    });
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
                    m_TextureEntryLock.AcquireReaderLock(() =>
                    {
                        foreach (var face in GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_COLOR")))
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Alpha, paramList);
                        }
                    });
                    break;

                case PrimitiveParamsType.AllowUnsit:
                    paramList.Add(AllowUnsit ? 1 : 0);
                    break;

                case PrimitiveParamsType.ScriptedSitOnly:
                    paramList.Add(IsScriptedSitOnly ? 1 : 0);
                    break;

                case PrimitiveParamsType.SitTarget:
                    paramList.Add(SitTargetOffset.ApproxEquals(Vector3.Zero, double.Epsilon) || SitTargetOrientation.ApproxEquals(Quaternion.Identity, double.Epsilon));
                    paramList.Add(SitTargetOffset);
                    paramList.Add(SitTargetOrientation);
                    break;

                case PrimitiveParamsType.Projector:
                    {
                        var param = Projection;
                        paramList.Add(param.IsProjecting ? 1 : 0);
                        paramList.Add(param.ProjectionTextureID);
                        paramList.Add(param.ProjectionFOV);
                        paramList.Add(param.ProjectionFocus);
                        paramList.Add(param.ProjectionAmbience);
                    }
                    break;

                case PrimitiveParamsType.ProjectorEnabled:
                    {
                        var param = Projection;
                        paramList.Add(param.IsProjecting ? 1 : 0);
                    }
                    break;

                case PrimitiveParamsType.ProjectorTexture:
                    {
                        var param = Projection;
                        paramList.Add(param.ProjectionTextureID);
                    }
                    break;

                case PrimitiveParamsType.ProjectorFov:
                    {
                        var param = Projection;
                        paramList.Add(param.ProjectionFOV);
                    }
                    break;

                case PrimitiveParamsType.ProjectorFocus:
                    {
                        var param = Projection;
                        paramList.Add(param.ProjectionFocus);
                    }
                    break;

                case PrimitiveParamsType.ProjectorAmbience:
                    {
                        var param = Projection;
                        paramList.Add(param.ProjectionAmbience);
                    }
                    break;

                default:
                    throw new LocalizedScriptErrorException(this, "PRIMInvalidParameterType0", "Invalid primitive parameter type {0}", enumerator.Current.AsUInt);
            }
        }

        public const int ALL_SIDES = -1;

        public int NumberOfSides => Shape.NumberOfSides;

        public ICollection<TextureEntryFace> GetFaces(int face)
        {
            if (face == ALL_SIDES)
            {
                var list = new List<TextureEntryFace>();
                for (uint i = 0; i < NumberOfSides; ++i)
                {
                    list.Add(m_TextureEntry[i]);
                }
                return list;
            }
            else
            {
                return new List<TextureEntryFace>
                {
                    m_TextureEntry[(uint)face]
                };
            }
        }

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
            UpdateChangedFlags flags = 0;
            bool isUpdated = false;
            switch (ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    Name = ParamsHelper.GetString(enumerator, "PRIM_NAME");
                    break;

                case PrimitiveParamsType.Desc:
                    Description = ParamsHelper.GetString(enumerator, "PRIM_DESC");
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
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_ALPHAMODE"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.AlphaMode, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Texture;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.Normal:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_NORMAL"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Normal, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Texture;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.Specular:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Specular, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Texture;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.Texture:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Texture, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Texture;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.Text:
                    {
                        var p = new TextParam()
                        {
                            Text = ParamsHelper.GetString(enumerator, "PRIM_TEXT")
                        };
                        Vector3 v = ParamsHelper.GetVector(enumerator, "PRIM_TEXT");
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_TEXT");
                        p.TextColor = new ColorAlpha(v, alpha);
                        Text = p;
                    }
                    break;

                case PrimitiveParamsType.Color:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Color, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Color;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.Alpha:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_ALPHA"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Alpha, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Color;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.BumpShiny:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.BumpShiny, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Texture;
                    });
                    break;

                case PrimitiveParamsType.PointLight:
                    {
                        PointLight = new PointLightParam()
                        {
                            IsLight = ParamsHelper.GetBoolean(enumerator, "PRIM_POINT_LIGHT"),
                            LightColor = new Color(ParamsHelper.GetVector(enumerator, "PRIM_POINT_LIGHT")),
                            Intensity = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT"),
                            Radius = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT"),
                            Falloff = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT")
                        };
                    }
                    break;

                case PrimitiveParamsType.FullBright:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.FullBright, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Color;
                    });
                    break;

                case PrimitiveParamsType.Flexible:
                    {
                        Flexible = new FlexibleParam()
                        {
                            IsFlexible = ParamsHelper.GetBoolean(enumerator, "PRIM_FLEXIBLE"),
                            Softness = ParamsHelper.GetInteger(enumerator, "PRIM_FLEXIBLE"),
                            Gravity = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE"),
                            Friction = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE"),
                            Wind = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE"),
                            Force = ParamsHelper.GetVector(enumerator, "PRIM_FLEXIBLE")
                        };
                    }
                    break;

                case PrimitiveParamsType.TexGen:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.TexGen, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Texture;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.Glow:
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        var faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW"));
                        enumerator.MarkPosition();
                        foreach (var face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Glow, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                        flags |= UpdateChangedFlags.Color;
                        isUpdated = true;
                    });
                    break;

                case PrimitiveParamsType.Omega:
                    {
                        Omega = new OmegaParam()
                        {
                            Axis = ParamsHelper.GetVector(enumerator, "PRIM_OMEGA"),
                            Spinrate = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA"),
                            Gain = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA")
                        };
                    }
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

                case PrimitiveParamsType.Projector:
                    {
                        var param = new ProjectionParam()
                        {
                            IsProjecting = ParamsHelper.GetBoolean(enumerator, "PRIM_PROJECTOR"),
                            ProjectionTextureID = GetTextureParam(enumerator, "PRIM_PROJECTOR"),
                            ProjectionFOV = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR"),
                            ProjectionFocus = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR"),
                            ProjectionAmbience = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR")
                        };
                    }
                    break;

                case PrimitiveParamsType.ProjectorEnabled:
                    {
                        var param = Projection;
                        param.IsProjecting = ParamsHelper.GetBoolean(enumerator, "PRIM_PROJECTOR_ENABLED");
                        Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorTexture:
                    {
                        var param = Projection;
                        param.ProjectionTextureID = GetTextureParam(enumerator, "PRIM_PROJECTOR_TEXTURE");
                        Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorFov:
                    {
                        var param = Projection;
                        param.ProjectionFOV = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR_FOV");
                        Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorFocus:
                    {
                        ProjectionParam param = Projection;
                        param.ProjectionFocus = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR_FOCUS");
                        Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorAmbience:
                    {
                        var param = Projection;
                        param.ProjectionAmbience = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTION_AMBIENCE");
                        Projection = param;
                    }
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
        private const int PRIM_ALPHA_MODE_BLEND = 1;

        private string GetTextureInventoryItem(UUID assetID)
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

        public void GetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, AnArray paramList)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    paramList.Add(GetTextureInventoryItem(face.TextureID));
                    paramList.Add(new Vector3(face.RepeatU, face.RepeatV, 0));
                    paramList.Add(new Vector3(face.OffsetU, face.OffsetV, 0));
                    paramList.Add(face.Rotation);
                    break;

                case PrimitiveParamsType.Color:
                    paramList.Add(face.TextureColor.AsVector3);
                    paramList.Add(1 - face.TextureColor.A);
                    break;

                case PrimitiveParamsType.Alpha:
                    paramList.Add(1 - face.TextureColor.A);
                    break;

                case PrimitiveParamsType.BumpShiny:
                    paramList.Add((int)face.Shiny);
                    paramList.Add((int)face.Bump);
                    break;

                case PrimitiveParamsType.FullBright:
                    paramList.Add(face.FullBright);
                    break;

                case PrimitiveParamsType.TexGen:
                    paramList.Add((int)face.TexMapType);
                    break;

                case PrimitiveParamsType.Glow:
                    paramList.Add(face.Glow);
                    break;

                case PrimitiveParamsType.AlphaMode:
                    /* [ PRIM_ALPHA_MODE, integer face, integer alpha_mode, integer mask_cutoff ] */
                    if (face.MaterialID == UUID.Zero)
                    {
                        paramList.Add(PRIM_ALPHA_MODE_BLEND);
                        paramList.Add(0);
                    }
                    else
                    {
                        try
                        {
                            var mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
                            paramList.Add(mat.DiffuseAlphaMode);
                            paramList.Add(mat.AlphaMaskCutoff);
                        }
                        catch
                        {
                            paramList.Add(PRIM_ALPHA_MODE_BLEND);
                            paramList.Add(0);
                        }
                    }
                    break;

                case PrimitiveParamsType.Normal:
                    /* [ PRIM_NORMAL, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians ] */
                    if (face.MaterialID == UUID.Zero)
                    {
                        paramList.Add(UUID.Zero);
                        paramList.Add(new Vector3(1, 1, 0));
                        paramList.Add(Vector3.Zero);
                        paramList.Add(0f);
                    }
                    else
                    {
                        try
                        {
                            var mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
                            paramList.Add(GetTextureInventoryItem(mat.NormMap));
                            paramList.Add(new Vector3(mat.NormRepeatX, mat.NormRepeatY, 0) / SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER);
                            paramList.Add(new Vector3(mat.NormOffsetX, mat.NormOffsetY, 0) / SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER);
                            paramList.Add(mat.NormRotation);
                        }
                        catch
                        {
                            paramList.Add(UUID.Zero);
                            paramList.Add(new Vector3(1, 1, 0));
                            paramList.Add(Vector3.Zero);
                            paramList.Add(0f);
                        }
                    }
                    break;

                case PrimitiveParamsType.Specular:
                    /* [ PRIM_SPECULAR, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians, vector color, integer glossiness, integer environment ] */
                    if (face.MaterialID == UUID.Zero)
                    {
                        paramList.Add(UUID.Zero);
                        paramList.Add(new Vector3(1, 1, 0));
                        paramList.Add(Vector3.Zero);
                        paramList.Add(0f);
                        paramList.Add(Vector3.One);
                        paramList.Add(0);
                        paramList.Add(0);
                    }
                    else
                    {
                        try
                        {
                            var mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
                            paramList.Add(GetTextureInventoryItem(mat.SpecMap));
                            paramList.Add(new Vector3(mat.SpecRepeatX, mat.SpecRepeatY, 0) / SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER);
                            paramList.Add(new Vector3(mat.SpecOffsetX, mat.SpecOffsetY, 0) / SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER);
                            paramList.Add(mat.SpecRotation / SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER);
                            paramList.Add(mat.SpecColor.AsVector3);
                            paramList.Add(mat.SpecExp);
                            paramList.Add(mat.EnvIntensity);
                        }
                        catch
                        {
                            paramList.Add(UUID.Zero);
                            paramList.Add(new Vector3(1, 1, 0));
                            paramList.Add(Vector3.Zero);
                            paramList.Add(0f);
                            paramList.Add(Vector3.One);
                            paramList.Add(0);
                            paramList.Add(0);
                        }
                    }
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }

        private UUID GetTextureParam(IEnumerator<IValue> enumerator, string paraName)
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

        public void SetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, AnArray.MarkEnumerator enumerator)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    {
                        face.TextureID = GetTextureParam(enumerator, "PRIM_TEXTURE");
                        Vector3 v = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        face.RepeatU = (float)v.X;
                        face.RepeatV = (float)v.Y;
                        v = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        face.OffsetU = (float)v.X;
                        face.OffsetV = (float)v.Y;
                        face.Rotation = (float)ParamsHelper.GetDouble(enumerator, "PRIM_TEXTURE");
                    }
                    break;

                case PrimitiveParamsType.Color:
                    {
                        Vector3 color = ParamsHelper.GetVector(enumerator, "PRIM_COLOR");
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_COLOR").Clamp(0, 1);
                        face.TextureColor = new ColorAlpha(color, 1 - alpha);
                    }
                    break;

                case PrimitiveParamsType.Alpha:
                    {
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_ALPHA").Clamp(0, 1);
                        face.TextureColor.A = 1 - alpha;
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    face.Shiny = (Shininess)ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                    face.Bump = (Bumpiness)ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                    break;

                case PrimitiveParamsType.FullBright:
                    face.FullBright = ParamsHelper.GetBoolean(enumerator, "PRIM_FULLBRIGHT");
                    break;

                case PrimitiveParamsType.TexGen:
                    face.TexMapType = (MappingType)ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN");
                    break;

                case PrimitiveParamsType.Glow:
                    face.Glow = (float)ParamsHelper.GetDouble(enumerator, "PRIM_GLOW");
                    break;

                case PrimitiveParamsType.AlphaMode:
                    /* [ PRIM_ALPHA_MODE, integer face, integer alpha_mode, integer mask_cutoff ] */
                    {
                        Material mat;
                        try
                        {
                            mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
                        }
                        catch
                        {
                            mat = new Material();
                        }
                        mat.DiffuseAlphaMode = ParamsHelper.GetInteger(enumerator, "PRIM_ALPHA_MODE");
                        mat.AlphaMaskCutoff = ParamsHelper.GetInteger(enumerator, "PRIM_ALPHA_MODE");
                        mat.DiffuseAlphaMode = mat.DiffuseAlphaMode.Clamp(0, 3);
                        mat.AlphaMaskCutoff = mat.AlphaMaskCutoff.Clamp(0, 3);
                        mat.MaterialID = UUID.Random;
                        ObjectGroup.Scene.StoreMaterial(mat);
                        face.MaterialID = mat.MaterialID;
                    }
                    break;

                case PrimitiveParamsType.Normal:
                    /* [ PRIM_NORMAL, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians ] */
                    {
                        UUID texture = GetTextureParam(enumerator, "PRIM_NORMAL");
                        Vector3 repeats = ParamsHelper.GetVector(enumerator, "PRIM_NORMAL");
                        Vector3 offsets = ParamsHelper.GetVector(enumerator, "PRIM_NORMAL");
                        double rotation = ParamsHelper.GetDouble(enumerator, "PRIM_NORMAL");

                        repeats.X *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        repeats.Y *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        offsets.X *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        offsets.Y *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        rotation %= Math.PI * 2;
                        rotation *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;

                        Material mat;
                        try
                        {
                            mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
                        }
                        catch
                        {
                            mat = new Material();
                        }
                        mat.NormMap = texture;
                        mat.NormOffsetX = (int)Math.Round(offsets.X);
                        mat.NormOffsetY = (int)Math.Round(offsets.Y);
                        mat.NormRepeatX = (int)Math.Round(repeats.X);
                        mat.NormRepeatY = (int)Math.Round(repeats.Y);
                        mat.NormRotation = (int)Math.Round(rotation);
                        mat.MaterialID = UUID.Random;
                        ObjectGroup.Scene.StoreMaterial(mat);
                        face.MaterialID = mat.MaterialID;
                    }
                    break;

                case PrimitiveParamsType.Specular:
                    /* [ PRIM_SPECULAR, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians, vector color, integer glossiness, integer environment ] */
                    {
                        UUID texture = GetTextureParam(enumerator, "PRIM_NORMAL");
                        Vector3 repeats = ParamsHelper.GetVector(enumerator, "PRIM_SPECULAR");
                        Vector3 offsets = ParamsHelper.GetVector(enumerator, "PRIM_SPECULAR");
                        repeats *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        offsets *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        double rotation = ParamsHelper.GetDouble(enumerator, "PRIM_SPECULAR");
                        rotation %= Math.PI * 2;
                        rotation *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        var color = new ColorAlpha(ParamsHelper.GetVector(enumerator, "PRIM_SPECULAR"), 1);
                        int glossiness = ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR");
                        int environment = ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR");
                        environment = environment.Clamp(0, 255);
                        glossiness = glossiness.Clamp(0, 255);
                        Material mat;
                        try
                        {
                            mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
                        }
                        catch
                        {
                            mat = new Material();
                        }
                        mat.SpecColor = color;
                        mat.SpecMap = texture;
                        mat.SpecOffsetX = (int)Math.Round(offsets.X);
                        mat.SpecOffsetY = (int)Math.Round(offsets.Y);
                        mat.SpecRepeatX = (int)Math.Round(repeats.X);
                        mat.SpecRepeatY = (int)Math.Round(repeats.Y);
                        mat.SpecRotation = (int)Math.Round(rotation);
                        mat.EnvIntensity = environment;
                        mat.SpecExp = glossiness;
                        mat.MaterialID = UUID.Random;
                        ObjectGroup.Scene.StoreMaterial(mat);
                        face.MaterialID = mat.MaterialID;
                    }
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }

        #endregion
    }
}
