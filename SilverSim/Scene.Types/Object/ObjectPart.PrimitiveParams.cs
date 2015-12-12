﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        public class PrimitiveShape
        {
            #region Constructor
            public PrimitiveShape()
            {
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
                    byte[] serialized = new byte[45];
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

                    PrimitiveShapeType primType = Type;
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
                                ret += 1;
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
                                ret += 1;
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
                                ret += 1;
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
                                ret += 1;
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
                                ret += 1;
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
                                ret += 1;
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
                                ret += 1;
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

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
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
            const double CutQuanta = 0.00002f;
            /** <summary>divides by 100</summary> */
            const double ScaleQuanta = 0.01f;
            /** <summary>divides by 100</summary> */
            const double ShearQuanta = 0.01f;
            /** <summary>divides by 100</summary> */
            const double TaperQuanta = 0.01f;
            /** <summary>0.015f</summary> */
            const double RevQuanta = 0.015f;
            /** <summary>divides by 50000</summary> */
            const double HollowQuanta = 0.00002f;


            public Decoded DecodedParams
            {
                get
                {
                    Decoded d = new Decoded();

                    d.ShapeType = Type;
                    d.SculptType = SculptType;
                    d.SculptMap = SculptMap;
                    d.IsSculptInverted = IsSculptInverted;
                    d.IsSculptMirrored = IsSculptMirrored;

                    #region Profile Params
                    d.ProfileBegin = (ProfileBegin * CutQuanta).Clamp(0f, 1f);
                    d.ProfileEnd = (ProfileEnd * CutQuanta).Clamp(0f, 1f);
                    d.IsOpen = (ProfileBegin != 0 || ProfileEnd != 50000);
                    d.ProfileShape = (PrimitiveProfileShape)(ProfileCurve & (byte)PrimitiveProfileShape.Mask);
                    d.HoleShape = (PrimitiveProfileHollowShape)(ProfileCurve & (byte)PrimitiveProfileHollowShape.Mask);
                    d.ProfileHollow = ((Type != PrimitiveShapeType.Box || Type != PrimitiveShapeType.Tube) && 
                        d.HoleShape == PrimitiveProfileHollowShape.Square) ?
                        (ProfileHollow * HollowQuanta).Clamp(0f, 0.7f) :
                        (ProfileHollow * HollowQuanta).Clamp(0f, 0.99f);
                    d.IsHollow = ProfileHollow > 0;
                    #endregion

                    #region Path Rarams
                    d.PathBegin = (PathBegin * CutQuanta).Clamp(0f, 1f);
                    d.PathEnd = ((100 - PathEnd) * CutQuanta).Clamp(0f, 1f);
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
                    d.RadiusOffset = (PathRadiusOffset * ScaleQuanta);
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
                    int sculptFlags = (int)SculptType;
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
                    double topshearx = (double)(sbyte)PathShearX / 100.0; // Fix negative values for PathShearX
                    double topsheary = (double)(sbyte)PathShearY / 100.0; // and PathShearY.

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
                PrimitiveShape shape = new PrimitiveShape();
                shape.Type = (PrimitiveShapeType)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
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
                    PrimitiveHoleShape holeShape = (PrimitiveHoleShape)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
                    if (holeShape != PrimitiveHoleShape.Circle &&
                        holeShape != PrimitiveHoleShape.Default &&
                        holeShape != PrimitiveHoleShape.Square &&
                        holeShape != PrimitiveHoleShape.Triangle)
                    {
                        holeShape = PrimitiveHoleShape.Default;
                    }
                    PrimitiveProfileShape profileShape = PrimitiveProfileShape.Circle;
                    PrimitiveExtrusion extrusion = PrimitiveExtrusion.Straight;
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

                    if (cut.X < 0f)
                    {
                        cut.X = 0f;
                    }
                    if (cut.X > 1f)
                    {
                        cut.X = 1f;
                    }
                    if (cut.Y < 0f)
                    {
                        cut.Y = 0f;
                    }
                    if (cut.Y > 1f)
                    {
                        cut.Y = 1f;
                    }
                    if (cut.Y - cut.X < 0.05f)
                    {
                        cut.Y = cut.Y - 0.05f;
                        if (cut.X < 0.0f)
                        {
                            cut.X = 0.0f;
                            cut.Y = 0.05f;
                        }
                    }
                    shape.ProfileBegin = (ushort)(50000 * cut.X);
                    shape.ProfileEnd = (ushort)(50000 * (1 - cut.Y));

                    if (hollow < 0f)
                    {
                        hollow = 0f;
                    }
                    // If the prim is a Cylinder, Prism, Sphere, Torus or Ring (or not a
                    // Box or Tube) and the hole shape is a square, hollow is limited to
                    // a max of 70%. The viewer performs its own check on this value but
                    // we need to do it here also so llGetPrimitiveParams can have access
                    // to the correct value.
                    if (profileShape != PrimitiveProfileShape.Square &&
                        holeShape == PrimitiveHoleShape.Square)
                    {
                        if (hollow > 0.70f)
                        {
                            hollow = 0.70f;
                        }
                    }
                    // Otherwise, hollow is limited to 95%.
                    else
                    {
                        if (hollow > 0.95f)
                        {
                            hollow = 0.95f;
                        }
                    }
                    shape.ProfileHollow = (ushort)(50000 * hollow);
                    if (twist.X < -1.0f)
                    {
                        twist.X = -1.0f;
                    }
                    if (twist.X > 1.0f)
                    {
                        twist.X = 1.0f;
                    }
                    if (twist.Y < -1.0f)
                    {
                        twist.Y = -1.0f;
                    }
                    if (twist.Y > 1.0f)
                    {
                        twist.Y = 1.0f;
                    }

                    double tempFloat = (100.0d * twist.X);
                    shape.PathTwistBegin = (sbyte)tempFloat;
                    tempFloat = (100.0d * twist.Y);
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

                            if (topSize.X < 0f)
                            {
                                topSize.X = 0f;
                            }
                            if (topSize.X > 2f)
                            {
                                topSize.X = 2f;
                            }
                            if (topSize.Y < 0f)
                            {
                                topSize.Y = 0f;
                            }
                            if (topSize.Y > 2f)
                            {
                                topSize.Y = 2f;
                            }
                            tempFloat = (float)(100.0d * (2.0d - topSize.X));
                            shape.PathScaleX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * (2.0d - topSize.Y));
                            shape.PathScaleY = (byte)tempFloat;
                            if (topShear.X < -0.5f)
                            {
                                topShear.X = -0.5f;
                            }
                            if (topShear.X > 0.5f)
                            {
                                topShear.X = 0.5f;
                            }
                            if (topShear.Y < -0.5f)
                            {
                                topShear.Y = -0.5f;
                            }
                            if (topShear.Y > 0.5f)
                            {
                                topShear.Y = 0.5f;
                            }
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

                            if (dimple.X < 0f)
                            {
                                dimple.X = 0f;
                            }
                            if (dimple.X > 1f)
                            {
                                dimple.X = 1f;
                            }
                            if (dimple.Y < 0f)
                            {
                                dimple.Y = 0f;
                            }
                            if (dimple.Y > 1f)
                            {
                                dimple.Y = 1f;
                            }
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

                            if (holeSize.X < 0.05f)
                            {
                                holeSize.X = 0.05f;
                            }
                            if (holeSize.X > 1f)
                            {
                                holeSize.X = 1f;
                            }
                            if (holeSize.Y < 0.05f)
                            {
                                holeSize.Y = 0.05f;
                            }
                            if (holeSize.Y > 0.5f)
                            {
                                holeSize.Y = 0.5f;
                            }
                            tempFloat = (float)(100.0d * (2.0d - holeSize.X));
                            shape.PathScaleX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * (2.0d - holeSize.Y));
                            shape.PathScaleY = (byte)tempFloat;
                            if (topShear.X < -0.5f)
                            {
                                topShear.X = -0.5f;
                            }
                            if (topShear.X > 0.5f)
                            {
                                topShear.X = 0.5f;
                            }
                            if (topShear.Y < -0.5f)
                            {
                                topShear.Y = -0.5f;
                            }
                            if (topShear.Y > 0.5f)
                            {
                                topShear.Y = 0.5f;
                            }
                            tempFloat = (float)(100.0d * topShear.X);
                            shape.PathShearX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * topShear.Y);
                            shape.PathShearY = (byte)tempFloat;
                            if (advancedCut.X < 0f)
                            {
                                advancedCut.X = 0f;
                            }
                            if (advancedCut.X > 1f)
                            {
                                advancedCut.X = 1f;
                            }
                            if (advancedCut.Y < 0f)
                            {
                                advancedCut.Y = 0f;
                            }
                            if (advancedCut.Y > 1f)
                            {
                                advancedCut.Y = 1f;
                            }
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
                            if (taper.X < -1f)
                            {
                                taper.X = -1f;
                            }
                            if (taper.X > 1f)
                            {
                                taper.X = 1f;
                            }
                            if (taper.Y < -1f)
                            {
                                taper.Y = -1f;
                            }
                            if (taper.Y > 1f)
                            {
                                taper.Y = 1f;
                            }
                            tempFloat = (float)(100.0d * taper.X);
                            shape.PathTaperX = (sbyte)tempFloat;
                            tempFloat = (float)(100.0d * taper.Y);
                            shape.PathTaperY = (sbyte)tempFloat;
                            if (revolutions < 1f)
                            {
                                revolutions = 1f;
                            }
                            if (revolutions > 4f)
                            {
                                revolutions = 4f;
                            }
                            tempFloat = 66.66667f * (revolutions - 1.0f);
                            shape.PathRevolutions = (byte)tempFloat;
                            // limits on radiusoffset depend on revolutions and hole size (how?) seems like the maximum range is 0 to 1
                            if (radiusOffset < 0f)
                            {
                                radiusOffset = 0f;
                            }
                            if (radiusOffset > 1f)
                            {
                                radiusOffset = 1f;
                            }
                            tempFloat = 100.0f * radiusOffset;
                            shape.PathRadiusOffset = (sbyte)tempFloat;
                            if (skew < -0.95f)
                            {
                                skew = -0.95f;
                            }
                            if (skew > 0.95f)
                            {
                                skew = 0.95f;
                            }
                            tempFloat = 100.0f * skew;
                            shape.PathSkew = (sbyte)tempFloat;
                            break;

                        default:
                            throw new ArgumentException(String.Format("Invalid primitive type {0}", shape.Type));
                    }
                }

                return shape;
            }
        }

        private readonly PrimitiveShape m_Shape = new PrimitiveShape();

        public PrimitiveShape Shape
        {
            get
            {
                PrimitiveShape res = new PrimitiveShape();
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

                    if (value.SculptType == SilverSim.Types.Primitive.PrimitiveSculptType.Mesh)
                    {
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin] = (byte)(12500 % 256);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin + 1] = (byte)(12500 / 256);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd] = 0;
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd + 1] = 0;
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow] = (byte)(27500 % 256);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow + 1] = (byte)(27500 / 256);
                    }
                }
                if (sculptChanged)
                {
                    UpdateExtraParams();
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        #region Primitive Methods
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
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
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_ALPHAMODE"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.AlphaMode, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Normal:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_NORMAL"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Normal, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Specular:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Specular, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Texture:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Text:
                    {
                        TextParam text = Text;
                        paramList.Add(text.TextColor.AsVector3);
                        paramList.Add(text.TextColor.A);
                    }
                    break;

                case PrimitiveParamsType.Color:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_COLOR"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Color, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.BumpShiny, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
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
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Flexible:
                    {
                        FlexibleParam p = Flexible;
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
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Glow:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Omega:
                    {
                        OmegaParam p = Omega;
                        paramList.Add(p.Axis);
                        paramList.Add(p.Spinrate);
                        paramList.Add(p.Gain);
                    }
                    break;

                case PrimitiveParamsType.Alpha:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_COLOR"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Alpha, paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Projector:
                    {
                        ProjectionParam param = Projection;
                        paramList.Add(param.IsProjecting ? 1 : 0);
                        paramList.Add(param.ProjectionTextureID);
                        paramList.Add(param.ProjectionFOV);
                        paramList.Add(param.ProjectionFocus);
                        paramList.Add(param.ProjectionAmbience);
                    }
                    break;

                case PrimitiveParamsType.ProjectorEnabled:
                    {
                        ProjectionParam param = Projection;
                        paramList.Add(param.IsProjecting ? 1 : 0);
                    }
                    break;

                case PrimitiveParamsType.ProjectorTexture:
                    {
                        ProjectionParam param = Projection;
                        paramList.Add(param.ProjectionTextureID);
                    }
                    break;

                case PrimitiveParamsType.ProjectorFov:
                    {
                        ProjectionParam param = Projection;
                        paramList.Add(param.ProjectionFOV);
                    }
                    break;

                case PrimitiveParamsType.ProjectorFocus:
                    {
                        ProjectionParam param = Projection;
                        paramList.Add(param.ProjectionFocus);
                    }
                    break;

                case PrimitiveParamsType.ProjectorAmbience:
                    {
                        ProjectionParam param = Projection;
                        paramList.Add(param.ProjectionAmbience);
                    }
                    break;

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsUInt));
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const int ALL_SIDES = -1;


        public int NumberOfSides
        {
            get
            {
                return Shape.NumberOfSides;
            }
        }

        public ICollection<TextureEntryFace> GetFaces(int face)
        {
            if (face == ALL_SIDES)
            {
                List<TextureEntryFace> list = new List<TextureEntryFace>();
                for (int i = 0; i < NumberOfSides; ++i)
                {
                    list.Add(m_TextureEntry[(uint)face]);
                }
                return list;
            }
            else
            {
                List<TextureEntryFace> list = new List<TextureEntryFace>();
                list.Add(m_TextureEntry[(uint)face]);
                return list;
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
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
                    Rotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROTATION");
                    break;

                case PrimitiveParamsType.RotLocal:
                    LocalRotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROT_LOCAL");
                    break;

                case PrimitiveParamsType.Size:
                    Size = ParamsHelper.GetVector(enumerator, "PRIM_SIZE");
                    break;

                case PrimitiveParamsType.AlphaMode:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_ALPHAMODE"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.AlphaMode, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Normal:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_NORMAL"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Normal, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Specular:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Specular, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Texture:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Texture, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Text:
                    {
                        TextParam p = new TextParam();
                        p.Text = ParamsHelper.GetString(enumerator, "PRIM_TEXT");
                        Vector3 v = ParamsHelper.GetVector(enumerator, "PRIM_TEXT");
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_TEXT");
                        p.TextColor = new ColorAlpha(v, alpha);
                        Text = p;
                    }
                    break;

                case PrimitiveParamsType.Color:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Color, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Alpha:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_ALPHA"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Alpha, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.BumpShiny, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.PointLight:
                    {
                        PointLightParam p = new PointLightParam();
                        p.IsLight = ParamsHelper.GetBoolean(enumerator, "PRIM_POINT_LIGHT");
                        p.LightColor = new Color(ParamsHelper.GetVector(enumerator, "PRIM_POINT_LIGHT"));
                        p.Intensity = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT");
                        p.Radius = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT");
                        p.Falloff = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT");
                        PointLight = p;
                    }
                    break;

                case PrimitiveParamsType.FullBright:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.FullBright, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Flexible:
                    {
                        FlexibleParam p = new FlexibleParam();
                        p.IsFlexible = ParamsHelper.GetBoolean(enumerator, "PRIM_FLEXIBLE");
                        p.Softness = ParamsHelper.GetInteger(enumerator, "PRIM_FLEXIBLE");
                        p.Gravity = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE");
                        p.Friction = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE");
                        p.Wind = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE");
                        p.Force = ParamsHelper.GetVector(enumerator, "PRIM_FLEXIBLE");
                        Flexible = p;
                    }
                    break;

                case PrimitiveParamsType.TexGen:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.TexGen, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Glow:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Texture, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Omega:
                    {
                        OmegaParam p = new OmegaParam();
                        p.Axis = ParamsHelper.GetVector(enumerator, "PRIM_OMEGA");
                        p.Spinrate = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA");
                        p.Gain = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA");
                        Omega = p;
                    }
                    break;

                case PrimitiveParamsType.Projector:
                    {
                        ProjectionParam param = new ProjectionParam();
                        param.IsProjecting = ParamsHelper.GetBoolean(enumerator, "PRIM_PROJECTOR");
                        param.ProjectionTextureID = GetTextureParam(enumerator, "PRIM_PROJECTOR");
                        param.ProjectionFOV = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR");
                        param.ProjectionFocus = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR");
                        param.ProjectionAmbience = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTOR");
                    }
                    break;

                case PrimitiveParamsType.ProjectorEnabled:
                    {
                        ProjectionParam param = Projection;
                        param.IsProjecting = ParamsHelper.GetBoolean(enumerator, "PRIM_PROJECTOR_ENABLED");
                        Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorTexture:
                    {
                        ProjectionParam param = Projection;
                        param.ProjectionTextureID = GetTextureParam(enumerator, "PRIM_PROJECTOR_TEXTURE");
                        Projection = param;
                    }
                    break;

                case PrimitiveParamsType.ProjectorFov:
                    {
                        ProjectionParam param = Projection;
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
                        ProjectionParam param = Projection;
                        param.ProjectionAmbience = ParamsHelper.GetDouble(enumerator, "PRIM_PROJECTION_AMBIENCE");
                        Projection = param;
                    }
                    break;

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsInt));
            }
        }
        #endregion

        #region TextureEntryFace functions
        const int PRIM_ALPHA_MODE_BLEND = 1;

        string GetTextureInventoryItem(UUID assetID)
        {
            if (assetID != UUID.Zero)
            {
                foreach (ObjectPartInventoryItem item in Inventory.Values)
                {
                    if (item.AssetType == AssetType.Texture && item.AssetID == assetID)
                    {
                        return item.Name;
                    }
                }
            }
            return assetID.ToString();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
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
                    paramList.Add(face.TextureColor.A);
                    break;

                case PrimitiveParamsType.Alpha:
                    paramList.Add(face.TextureColor.A);
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
                    else try
                    {
                        Material mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
                        paramList.Add(mat.DiffuseAlphaMode);
                        paramList.Add(mat.AlphaMaskCutoff);
                    }
                    catch
                    {
                        paramList.Add(PRIM_ALPHA_MODE_BLEND);
                        paramList.Add(0);
                    }
                    break;

                case PrimitiveParamsType.Normal:
                    /* [ PRIM_NORMAL, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians ] */
                    if(face.MaterialID == UUID.Zero)
                    {
                        paramList.Add(UUID.Zero);
                        paramList.Add(new Vector3(1, 1, 0));
                        paramList.Add(Vector3.Zero);
                        paramList.Add(0f);
                    }
                    else try
                    {
                        Material mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
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
                    else try
                    {
                        Material mat = ObjectGroup.Scene.GetMaterial(face.MaterialID);
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
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }

        UUID GetTextureParam(IEnumerator<IValue> enumerator, string paraName)
        {
            string texture = ParamsHelper.GetString(enumerator, paraName);
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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void SetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, AnArray.MarkEnumerator enumerator)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    {
                        face.TextureID = GetTextureParam(enumerator, "PRIM_TEXTURE");
                        Vector3 v;
                        v = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
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
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_COLOR");
                        face.TextureColor = new ColorAlpha(color, alpha);
                    }
                    break;

                case PrimitiveParamsType.Alpha:
                    {
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_ALPHA");
                        face.TextureColor.A = alpha;
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
                        if(mat.DiffuseAlphaMode < 0)
                        {
                            mat.DiffuseAlphaMode = 0;
                        }
                        else if(mat.DiffuseAlphaMode > 3)
                        {
                            mat.DiffuseAlphaMode = 3;
                        }
                        if(mat.AlphaMaskCutoff < 0)
                        {
                            mat.AlphaMaskCutoff = 0;
                        }
                        else if(mat.AlphaMaskCutoff > 3)
                        {
                            mat.AlphaMaskCutoff = 3;
                        }
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
                        rotation %= (Math.PI * 2);
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
                        repeats = repeats * SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        offsets = offsets * SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        double rotation = ParamsHelper.GetDouble(enumerator, "PRIM_SPECULAR");
                        rotation %= (Math.PI * 2);
                        rotation *= SilverSim.Types.Asset.Format.Material.MATERIALS_MULTIPLIER;
                        ColorAlpha color = new ColorAlpha(ParamsHelper.GetVector(enumerator, "PRIM_SPECULAR"), 1);
                        int glossiness = ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR");
                        int environment = ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR");
                        if(environment < 0)
                        {
                            environment = 0;
                        }
                        else if(environment > 255)
                        {
                            environment = 255;
                        }
                        if(glossiness < 0)
                        {
                            glossiness = 0;
                        }
                        else if(glossiness > 255)
                        {
                            glossiness = 255;
                        }
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
