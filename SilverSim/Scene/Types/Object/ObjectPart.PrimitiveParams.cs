/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

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
            public PrimitiveShapeType Type = PrimitiveShapeType.Box;

            public UUID SculptMap = UUID.Zero;
            public PrimitiveSculptType SculptType = PrimitiveSculptType.Sphere;
            public bool IsSculptInverted = false;
            public bool IsSculptMirrored = false;

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
                    if (primType == PrimitiveShapeType.Box
                        ||
                        primType == PrimitiveShapeType.Cylinder
                        ||
                        primType == PrimitiveShapeType.Prism)
                    {

                        hasCut = (ProfileBegin > 0) || (ProfileEnd > 0);
                    }
                    else
                    {
                        hasCut = (PathBegin > 0) || (PathEnd > 0);
                    }

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
                            if (SculptType == PrimitiveSculptType.Mesh)
                            {
                                ret = 32; // if it's a mesh then max 32 faces
                            }
                            else
                            {
                                ret = 1; // if it's a sculpt then max 1 face
                            }
                            break;
                    }

                    return ret;
                }
            }
            public ushort PathBegin;
            public byte PathCurve;
            public ushort PathEnd;
            public sbyte PathRadiusOffset;
            public byte PathRevolutions;
            public byte PathScaleX;
            public byte PathScaleY;
            public byte PathShearX;
            public byte PathShearY;
            public sbyte PathSkew;
            public sbyte PathTaperX;
            public sbyte PathTaperY;
            public sbyte PathTwist;
            public sbyte PathTwistBegin;
            public ushort ProfileBegin;
            public byte ProfileCurve;
            public ushort ProfileEnd;
            public ushort ProfileHollow;

            public PrimitiveCode PCode;

            public byte State;

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

            #endregion
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
                if (sculptChanged)
                {
                    UpdateExtraParams();
                }
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Shape);
            }
        }

        #region Primitive Methods
        public void GetPrimitiveParams(AnArray.Enumerator enumerator, ref AnArray paramList)
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

                case PrimitiveParamsType.Texture:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
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
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Color, ref paramList);
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
                            GetTexPrimitiveParams(face, PrimitiveParamsType.BumpShiny, ref paramList);
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
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
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
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
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
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
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

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsUInt));
            }
        }

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

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsInt));
            }
        }
        #endregion

        #region TextureEntryFace functions
        public void GetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, ref AnArray paramList)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    paramList.Add(face.TextureID);
                    paramList.Add(new Vector3(face.RepeatU, face.RepeatV, 0));
                    paramList.Add(new Vector3(face.OffsetU, face.OffsetV, 0));
                    paramList.Add(face.Rotation);
                    break;

                case PrimitiveParamsType.Color:
                    paramList.Add(face.TextureColor.AsVector3);
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

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }

        public void SetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, AnArray.MarkEnumerator enumerator)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    {
                        face.TextureID = ParamsHelper.GetString(enumerator, "PRIM_TEXTURE");
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

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }
        #endregion
    }
}
