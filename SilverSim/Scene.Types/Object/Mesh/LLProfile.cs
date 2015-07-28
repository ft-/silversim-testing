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

===============================================================================
The following code segment is based on SL viewer code which is distributed under LGPL V2.1.

*/

using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object.Mesh
{
    class LLProfile
    {
        [Flags]
        public enum FaceID : ushort
        {
            PathBegin = 0x1 << 0,
            PathEnd = 0x1 << 1,
            InnerSide = 0x1 << 2,
            ProfileBegin = 0x1 << 3,
            ProfileEnd = 0x1 << 4,
            OuterSide0 = 0x1 << 5,
            OuterSide1 = 0x1 << 6,
            OuterSide2 = 0x1 << 7,
            OuterSide3 = 0x1 << 8
        }

        public bool IsConcave { get; private set; }
        public bool IsOpen { get; private set; }

        public readonly List<Vector3> Profile = new List<Vector3>();

        public struct Face
        {
            public int Index;
            public int Count;
            public float ScaleU;
            public bool Cap;
            public bool Flat;
            public FaceID FaceID;
        }
        public readonly List<Face> Faces = new List<Face>();

        int TotalOut;

        const int MIN_DETAIL_FACES = 6;

        public LLProfile(ObjectPart.PrimitiveShape.Decoded shape, bool path_open, double detail, int split, bool is_sculpted, int sculpt_size)
        {
            int i;
            double begin = shape.ProfileBegin;
            double end = shape.ProfileEnd;
            double hollow = shape.ProfileHollow;
            if (begin > end - 0.01f)
            {
                throw new Exception();
            }

            int face_num = 0;
            TotalOut = 0;

            switch (shape.ProfileShape)
            {
                case PrimitiveProfileShape.Square:
                    {
                        GenNGon(shape, 4, -0.375f, 0f, 1f, split);
                        if (path_open)
                        {
                            addCap(FaceID.PathBegin);
                        }

                        for (i = (int)Math.Floor(begin * 4f); i < (int)Math.Floor(end * 4f + .999f); ++i)
                        {
                            addFace((face_num++) * (split + 1), split + 2, 1, (FaceID)((ushort)FaceID.OuterSide0 << i), true);
                        }

                        for (i = 0; i < Profile.Count; ++i)
                        {
                            Profile[i] = Profile[i] * 4f;
                        }

                        if (shape.ProfileHollow > 0)
                        {
                            switch (shape.HoleShape)
                            {
                                case PrimitiveProfileHollowShape.Triangle:
                                    addHole(shape, true, 3, -0.375f, hollow, 1f, split);
                                    break;

                                case PrimitiveProfileHollowShape.Circle:
                                    addHole(shape, false, MIN_DETAIL_FACES * detail, -0.375f, hollow, 1f);
                                    break;

                                case PrimitiveProfileHollowShape.Same:
                                case PrimitiveProfileHollowShape.Square:
                                default:
                                    addHole(shape, true, 4, -0.375f, hollow, 1f, split);
                                    break;
                            }
                        }
                    }
                    break;

                case PrimitiveProfileShape.IsometricTriangle:
                case PrimitiveProfileShape.RightTriangle:
                case PrimitiveProfileShape.EquilateralTriangle:
                    {
                        GenNGon(shape, 3, 0, 0, 1, split);
                        for (i = 0; i < Profile.Count; ++i)
                        {
                            Profile[i] = Profile[i] * 3f;
                        }

                        if (path_open)
                        {
                            addCap(FaceID.PathBegin);
                        }

                        for (i = (int)Math.Floor(begin * 3f); i < (int)Math.Floor(end * 3f + .999f); ++i)
                        {
                            addFace((face_num++) * (split + 1), split + 2, 1, (FaceID)((ushort)FaceID.OuterSide0 << i), true);
                        }
                        if (shape.ProfileHollow > 0)
                        {
                            double triangle_hollow = hollow / 2f;

                            switch (shape.HoleShape)
                            {
                                case PrimitiveProfileHollowShape.Circle:
                                    addHole(shape, false, MIN_DETAIL_FACES * detail, 0, triangle_hollow, 1f);
                                    break;

                                case PrimitiveProfileHollowShape.Square:
                                    addHole(shape, true, 4, 0, triangle_hollow, 1f, split);
                                    break;

                                case PrimitiveProfileHollowShape.Same:
                                case PrimitiveProfileHollowShape.Triangle:
                                default:
                                    addHole(shape, true, 3, 0, triangle_hollow, 1f, split);
                                    break;
                            }
                        }
                    }
                    break;

                case PrimitiveProfileShape.Circle:
                    {
                        PrimitiveProfileHollowShape hole_type = 0;
                        double circle_detail = MIN_DETAIL_FACES * detail;
                        if (shape.IsHollow)
                        {
                            hole_type = shape.HoleShape;
                            if (hole_type == PrimitiveProfileHollowShape.Square)
                            {
                                circle_detail = Math.Ceiling(circle_detail / 4f) * 4f;
                            }
                        }

                        int sides = (int)circle_detail;
                        if (is_sculpted)
                        {
                            sides = sculpt_size;
                        }

                        GenNGon(shape, sides);

                        if (path_open)
                        {
                            addCap(FaceID.PathBegin);
                        }

                        if (IsOpen && 0 == shape.ProfileHollow)
                        {
                            addFace(0, Profile.Count - 1, 0, FaceID.OuterSide0, false);
                        }
                        else
                        {
                            addFace(0, Profile.Count, 0, FaceID.OuterSide0, false);
                        }

                        if (shape.ProfileHollow > 0)
                        {
                            switch (hole_type)
                            {
                                case PrimitiveProfileHollowShape.Square:
                                    addHole(shape, true, 4, 0, hollow, 1f, split);
                                    break;

                                case PrimitiveProfileHollowShape.Triangle:
                                    addHole(shape, true, 3, 0, hollow, 1f, split);
                                    break;

                                case PrimitiveProfileHollowShape.Circle:
                                case PrimitiveProfileHollowShape.Same:
                                default:
                                    addHole(shape, false, circle_detail, 0, hollow, 1f);
                                    break;
                            }
                        }
                    }
                    break;

                case PrimitiveProfileShape.HalfCircle:
                    {
                        PrimitiveProfileHollowShape hole_type = 0;
                        double circle_detail = MIN_DETAIL_FACES * detail * 0.5f;
                        if (shape.ProfileHollow > 0)
                        {
                            hole_type = shape.HoleShape;
                            if (hole_type == PrimitiveProfileHollowShape.Square)
                            {
                                circle_detail = Math.Ceiling(circle_detail / 2f) * 2f;
                            }
                        }

                        GenNGon(shape, (int)Math.Floor(circle_detail), 0.5f, 0f, 0.5f);
                        if (path_open)
                        {
                            addCap(FaceID.PathBegin);
                        }
                        if (IsOpen && 0 == shape.ProfileHollow)
                        {
                            addFace(0, Profile.Count - 1, 0, FaceID.OuterSide0, false);
                        }
                        else
                        {
                            addFace(0, Profile.Count, 0, FaceID.OuterSide0, false);
                        }
                        if (shape.ProfileHollow > 0)
                        {
                            switch (hole_type)
                            {
                                case PrimitiveProfileHollowShape.Square:
                                    addHole(shape, true, 2, 0.5f, hollow, 0.5f, split);
                                    break;

                                case PrimitiveProfileHollowShape.Triangle:
                                    addHole(shape, true, 3, 0.5f, hollow, 0.5f, split);
                                    break;

                                case PrimitiveProfileHollowShape.Circle:
                                case PrimitiveProfileHollowShape.Same:
                                default:
                                    addHole(shape, false, circle_detail, 0.5f, hollow, 0.5f);
                                    break;
                            }
                        }

                        if ((int)shape.ProfileEnd - (int)shape.ProfileBegin < 50000)
                        {
                            IsOpen = true;
                        }
                        else if (0 == shape.ProfileHollow)
                        {
                            IsOpen = false;
                            Profile.Add(Profile[0]);
                        }
                    }
                    break;

                default:
                    throw new Exception();
            }

            if (path_open)
            {
                addCap(FaceID.PathEnd);
            }

            if (IsOpen)
            {
                addFace(Profile.Count - 1, 2, 0.5f, FaceID.ProfileBegin, true);
                if (shape.ProfileHollow > 0)
                {
                    addFace(TotalOut - 1, 2, 0.5f, FaceID.ProfileEnd, true);
                }
                else
                {
                    addFace(Profile.Count - 2, 2, 0.5f, FaceID.ProfileEnd, true);
                }
            }
        }


        void addCap(FaceID faceID)
        {
            Face f = new Face();
            f.Index = 0;
            f.Count = Profile.Count;
            f.ScaleU = 1;
            f.Cap = true;
            f.FaceID = faceID;
            Faces.Add(f);
        }

        void addFace(int i, int count, float ScaleU, FaceID faceID, bool flat)
        {
            Face f = new Face();
            f.Index = i;
            f.Count = count;
            f.ScaleU = ScaleU;
            f.Flat = flat;
            f.Cap = false;
            f.FaceID = faceID;
            Faces.Add(f);
        }

        readonly double[] TableScale = new double[] { 1, 1, 1, 0.5f, 0.707107f, 0.53f, 0.525f, 0.5f };

        void GenNGon(ObjectPart.PrimitiveShape.Decoded shape, int sides, double offset = 0f, double bevel = 0f, double ang_scale = 1f, int split = 0)
        {
            double scale = 0.5f;
            double t, t_step, t_first, t_fraction, ang, ang_step;
            Vector3 pt1, pt2;

            double begin = shape.ProfileBegin;
            double end = shape.ProfileEnd;
            t_step = 1f / sides;
            ang_step = 2f * Math.PI * t_step * ang_scale;

            int total_sides = (int)Math.Round(sides / ang_scale);

            if (total_sides < 8)
            {
                scale = TableScale[total_sides];
            }

            t_first = Math.Floor(begin * sides) / sides;

            t = t_first;
            ang = 2f * Math.PI * (t * ang_scale + offset);
            pt1 = new Vector3(Math.Cos(ang) * scale, Math.Sin(ang) * scale, t);

            t += t_step;
            ang += ang_step;
            pt2 = new Vector3(Math.Cos(ang) * scale, Math.Sin(ang) * scale, t);

            t_fraction = (begin - t_first) * sides;

            if (t_fraction < 0.9999f)
            {
                Profile.Add(Vector3.Lerp(pt1, pt2, t_fraction));
            }

            while (t < end)
            {
                pt1 = new Vector3(Math.Cos(ang) * scale, Math.Sin(ang) * scale, t);

                if (Profile.Count > 0)
                {
                    Vector3 p = Profile[Profile.Count - 1];
                    for (int i = 0; i < split; ++i)
                    {
                        Vector3 new_pt;
                        new_pt = pt1 - p;
                        new_pt = new_pt * (1f / (split + 1) * (i + 1));
                        new_pt = new_pt + p;
                        Profile.Add(new_pt);
                    }
                }
                Profile.Add(pt1);
            }

            t_fraction = (end - (t - t_step)) * sides;

            pt2 = new Vector3(Math.Cos(ang) * scale, Math.Sin(ang) * scale, t);

            t_fraction = (end - (t - t_step)) * sides;
            if (t_fraction > 0.0001f)
            {
                Vector3 new_pt = Vector3.Lerp(pt1, pt2, t_fraction);

                if (Profile.Count > 0)
                {
                    Vector3 p = Profile[Profile.Count - 1];
                    for (int i = 0; i < split; ++i)
                    {
                        Vector3 pt1a;
                        pt1a = new_pt - p;
                        pt1a *= (1f / (split + 1) * (i + 1));
                        pt1a += p;
                        Profile.Add(pt1a);
                    }
                }
            }

            if ((end - begin) * ang_scale < 0.99f)
            {
                IsConcave = ((end - begin) * ang_scale > 0.5);
                IsOpen = true;
                if (shape.ProfileHollow == 0)
                {
                    Profile.Add(Vector3.Zero);
                }
            }
            else
            {
                IsOpen = false;
                IsConcave = false;
            }
        }

        void addHole(ObjectPart.PrimitiveShape.Decoded shape, bool flat, double sides, double offset, double box_hollow, double ang_scale, int split = 0)
        {
            TotalOut = Profile.Count;
            GenNGon(shape, (int)Math.Floor(sides), offset, -1f, ang_scale, split);

            addFace(TotalOut, Profile.Count - TotalOut, 0, FaceID.InnerSide, flat);
            Vector3[] pt = new Vector3[Profile.Count];

            for (int i = TotalOut; i < Profile.Count; ++i)
            {
                pt[i] = Profile[i];
                pt[i] = pt[i] * box_hollow;
            }

            int j = Profile.Count - 1;
            for (int i = TotalOut; i < Profile.Count; ++i)
            {
                Profile[i] = pt[j--];
            }

            for (int i = 0; i < Faces.Count; ++i)
            {
                if (Faces[i].Cap)
                {
                    Face f = Faces[i];
                    f.Count *= 2;
                    Faces[i] = f;
                }
            }
        }
    }
}
