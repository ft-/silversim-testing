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
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        #region Box/Cylinder/Prism calculation
        private static void CalcTopSizeAndShear(ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut, out Vector3 topSize, out Vector3 shear, out double twist)
        {
            twist = twistBegin.Lerp(twistEnd, cut);
            topSize = new Vector3();
            shear = new Vector3();

            #region cut
            topSize.X = shape.PathScale.X < 0f ?
                1.0.Lerp(1f + shape.PathScale.X, 1f - cut) :
                1.0.Lerp(1f - shape.PathScale.X, cut);

            topSize.Y = shape.PathScale.Y < 0f ?
                1.0.Lerp(1f + shape.PathScale.Y, 1f - cut) :
                1.0.Lerp(1f - shape.PathScale.Y, cut);
            #endregion

            #region top_shear
            shear.X = shape.TopShear.X < 0f ?
                0.0.Lerp(shape.TopShear.X, cut) :
                0.0.Lerp(shape.TopShear.X, 1f - cut);

            shear.Y = shape.TopShear.Y < 0f ?
                0.0.Lerp(shape.TopShear.Y, cut) :
                0.0.Lerp(shape.TopShear.Y, 1f - cut);
            #endregion
        }

        private static Vector3 ApplyTortureParams(ObjectPart.PrimitiveShape.Decoded shape, Vector3 v, double twistBegin, double twistEnd, double cut)
        {
            double twist;
            Vector3 topSize;
            Vector3 shear;
            CalcTopSizeAndShear(shape, twistBegin, twistEnd, cut, out topSize, out shear, out twist);

            Vector3 outvertex = v;
            outvertex.X *= topSize.X;
            outvertex.Y *= topSize.Y;
            outvertex = outvertex.Rotate2D_XY(twist);
            outvertex += shear;
            outvertex.Z = cut - 0.5;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Torus:
                case PrimitiveShapeType.Tube:
                case PrimitiveShapeType.Ring:
                    outvertex.Z = outvertex.X;
                    outvertex.X = 0;
                    break;

                default:
                    break;
            }

            return outvertex;
        }

        private static List<Vector3> ExtrudeBasic(this ProfileDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            var extrusionPath = new List<Vector3>();
            double twist;
            Vector3 topSize;
            Vector3 shear;
            CalcTopSizeAndShear(shape, twistBegin, twistEnd, cut, out topSize, out shear, out twist);

            /* generate extrusions */
            foreach (var vertex in path.Vertices)
            {
                var outvertex = vertex;
                outvertex.X *= topSize.X;
                outvertex.Y *= topSize.Y;
                outvertex = outvertex.Rotate2D_XY(twist);
                outvertex += shear;
                outvertex.Z = cut - 0.5;
                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }

        private static MeshLOD BoxCylPrismToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            ProfileDetails path;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Box:
                    path = CalcBoxProfile(shape);
                    break;

                case PrimitiveShapeType.Cylinder:
                    path = CalcCylinderProfile(shape);
                    break;

                case PrimitiveShapeType.Prism:
                    path = CalcPrismProfile(shape);
                    break;

                default:
                    throw new NotImplementedException();
            }

            double cut = shape.PathBegin;
            double cutBegin = cut;
            double cutEnd = shape.PathEnd;
            double cutStep = (cutEnd - cut) / 10f;
            double twistBegin = shape.TwistBegin * Math.PI;
            double twistEnd = shape.TwistEnd * Math.PI;
            double neededSteps = Math.Max(1, Math.Ceiling((shape.TwistEnd - shape.TwistBegin) / (5 * Math.PI / 180) * (cutEnd - cut)));
            cutStep /= neededSteps;

            var mesh = new MeshLOD();
            if (double.Epsilon <= Math.Abs(shape.TwistBegin - shape.TwistEnd))
            {
                for (; cut < cutEnd; cut += cutStep)
                {
                    mesh.Vertices.AddRange(path.ExtrudeBasic(shape, twistBegin, twistEnd, cut));
                }
            }
            else
            {
                mesh.Vertices.AddRange(path.ExtrudeBasic(shape, twistBegin, twistEnd, cutBegin));
            }
            mesh.Vertices.AddRange(path.ExtrudeBasic(shape, twistBegin, twistEnd, cutEnd));

            shape.BuildBasicTriangles(mesh, path, cutBegin, cutEnd);
            return mesh;
        }

        private static void BuildBasicTriangles(this ObjectPart.PrimitiveShape.Decoded shape, MeshLOD mesh, ProfileDetails path, double cutBegin, double cutEnd)
        {
            double twistBegin = shape.TwistBegin * Math.PI;
            double twistEnd = shape.TwistEnd * Math.PI;
            int verticeRowCount = path.Vertices.Count;
            int verticeTotalCount = mesh.Vertices.Count;
            int verticeRowEndCount = verticeRowCount;
            if (!shape.IsOpen && shape.IsHollow)
            {
                --verticeRowEndCount;
            }

            /* generate z-triangles */
            for (int l = 0; l < verticeRowEndCount; ++l)
            {
                if (!shape.IsOpen && shape.IsHollow && l == verticeRowCount / 2 - 1)
                {
                    continue;
                }
                for (int z = 0; z < verticeTotalCount - verticeRowCount; z += verticeRowCount)
                {
                    /* p0  ___  p1 */
                    /*    |   |    */
                    /*    |___|    */
                    /* p3       p2 */
                    /* tris: p0, p1, p2 and p0, p3, p2 */
                    /* p2 and p3 are on next row */
                    int z2 = z + verticeRowCount;
                    int l2 = (l + 1) % verticeRowCount; /* loop closure */
                    var tri = new Triangle
                    {
                        Vertex1 = z + l,
                        Vertex2 = z2 + l2,
                        Vertex3 = z + l2
                    };
                    mesh.Triangles.Add(tri);

                    tri = new Triangle
                    {
                        Vertex1 = z + l,
                        Vertex2 = z2 + l,
                        Vertex3 = z2 + l2
                    };
                    mesh.Triangles.Add(tri);
                }
            }

            /* generate top and bottom triangles */
            if (shape.IsHollow)
            {
                /* simpler just close neighboring dots */
                /* no need for uneven check here.
                 * The path generator always generates two pathes here which are connected and therefore always a multiple of two */
                int bottomIndex = verticeTotalCount - verticeRowCount;
                for (int l = 0; l < verticeRowCount / 2; ++l)
                {
                    int l2 = verticeRowCount - l - 1;

                    var tri = new Triangle
                    {
                        Vertex1 = l,
                        Vertex2 = l2,
                        Vertex3 = l + 1
                    };
                    mesh.Triangles.Add(tri);

                    tri = new Triangle
                    {
                        Vertex1 = l + 1,
                        Vertex2 = l2,
                        Vertex3 = l2 - 1
                    };
                    mesh.Triangles.Add(tri);

                    tri = new Triangle
                    {
                        Vertex1 = l + bottomIndex,
                        Vertex2 = l2 + bottomIndex,
                        Vertex3 = l + 1 + bottomIndex
                    };
                    mesh.Triangles.Add(tri);

                    tri = new Triangle
                    {
                        Vertex1 = l + 1 + bottomIndex,
                        Vertex2 = l2 + bottomIndex,
                        Vertex3 = l2 - 1 + bottomIndex
                    };
                    mesh.Triangles.Add(tri);
                }
            }
            else
            {
                /* build a center point and connect all vertexes with triangles */
                int centerpointTop = mesh.Vertices.Count;
                int bottomIndex = verticeTotalCount - verticeRowCount;
                mesh.Vertices.Add(ApplyTortureParams(shape, new Vector3(0, 0, 0), twistBegin, twistEnd, cutBegin));
                int centerpointBottom = mesh.Vertices.Count;
                mesh.Vertices.Add(ApplyTortureParams(shape, new Vector3(0, 0, 0), twistBegin, twistEnd, cutEnd));
                for (int l = 0; l < verticeRowCount; ++l)
                {
                    int l2 = (l + 1) % verticeRowCount;

                    var tri = new Triangle
                    {
                        Vertex1 = l,
                        Vertex2 = l2,
                        Vertex3 = centerpointTop
                    };
                    mesh.Triangles.Add(tri);

                    tri = new Triangle
                    {
                        Vertex1 = l + bottomIndex,
                        Vertex2 = l2 + bottomIndex,
                        Vertex3 = centerpointBottom
                    };
                    mesh.Triangles.Add(tri);
                }
            }
        }
        #endregion

        #region 2D Path calculation
        private static Vector3 Rotate2D_XY(this Vector3 vec, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            return new Vector3(
                vec.X * cos - vec.Y * sin,
                vec.X * sin + vec.Y * cos,
                vec.Z);
        }

        private static Vector3 Rotate2D_YZ(this Vector3 vec, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            return new Vector3(
                vec.X,
                vec.Y * cos - vec.Z * sin,
                vec.Y * sin + vec.Z * cos);
        }

        private static double CalcEquA(Vector3 v1, Vector3 v2)
        {
            return v1.Y - v2.Y;
        }

        private static double CalcEquB(Vector3 v1, Vector3 v2)
        {
            return v2.X - v1.X;
        }

        private static double CalcEquD(Vector3 v1, Vector3 v2)
        {
            return CalcEquA(v1, v2) * v1.X + CalcEquB(v1, v2) * v1.Y;
        }

        private static double CalcTriBaseAngle(Vector3 v)
        {
            double ang = Math.Atan2(v.Y, v.X) - Math.Atan2(0.5, 0);
            while(ang < 0)
            {
                ang += 2 * Math.PI;
            }
            return ang;
        }

        private static readonly Vector3 TRIANGLE_P0 = Vector3.UnitX * 0.5;
        private static readonly Vector3 TRIANGLE_P1 = Vector3.UnitX.Rotate2D_XY(2.0943977032870302) * 0.5;
        private static readonly Vector3 TRIANGLE_P2 = Vector3.UnitX.Rotate2D_XY(-2.0943977032870302) * 0.5;
        private static readonly double[] TriEqu_A = new double[3] { CalcEquA(TRIANGLE_P0, TRIANGLE_P1), CalcEquA(TRIANGLE_P1, TRIANGLE_P2), CalcEquA(TRIANGLE_P2, TRIANGLE_P0) };
        private static readonly double[] TriEqu_B = new double[3] { CalcEquB(TRIANGLE_P0, TRIANGLE_P1), CalcEquB(TRIANGLE_P1, TRIANGLE_P2), CalcEquB(TRIANGLE_P2, TRIANGLE_P0) };
        private static readonly double[] TriEqu_D = new double[3] { CalcEquD(TRIANGLE_P0, TRIANGLE_P1), CalcEquD(TRIANGLE_P1, TRIANGLE_P2), CalcEquD(TRIANGLE_P2, TRIANGLE_P0) };

        private static readonly Vector3 TOPEDGE_SQUARE_P0 = Vector3.UnitX * 0.5 / 0.7;
        private static readonly Vector3 TOPEDGE_SQUARE_P1 = Vector3.UnitY * 0.5 / 0.7;
        private static readonly Vector3 TOPEDGE_SQUARE_P2 = -Vector3.UnitX * 0.5 / 0.7;
        private static readonly Vector3 TOPEDGE_SQUARE_P3 = -Vector3.UnitY * 0.5 / 0.7;

        private static readonly double[] TopEdgeEqu_A = new double[4] { CalcEquA(TOPEDGE_SQUARE_P0, TOPEDGE_SQUARE_P1), CalcEquA(TOPEDGE_SQUARE_P1, TOPEDGE_SQUARE_P2), CalcEquA(TOPEDGE_SQUARE_P2, TOPEDGE_SQUARE_P3), CalcEquA(TOPEDGE_SQUARE_P3, TOPEDGE_SQUARE_P0) };
        private static readonly double[] TopEdgeEqu_B = new double[4] { CalcEquB(TOPEDGE_SQUARE_P0, TOPEDGE_SQUARE_P1), CalcEquB(TOPEDGE_SQUARE_P1, TOPEDGE_SQUARE_P2), CalcEquB(TOPEDGE_SQUARE_P2, TOPEDGE_SQUARE_P3), CalcEquB(TOPEDGE_SQUARE_P3, TOPEDGE_SQUARE_P0) };
        private static readonly double[] TopEdgeEqu_D = new double[4] { CalcEquD(TOPEDGE_SQUARE_P0, TOPEDGE_SQUARE_P1), CalcEquD(TOPEDGE_SQUARE_P1, TOPEDGE_SQUARE_P2), CalcEquD(TOPEDGE_SQUARE_P2, TOPEDGE_SQUARE_P3), CalcEquD(TOPEDGE_SQUARE_P3, TOPEDGE_SQUARE_P0) };
        private static readonly double TopEdgeAngSeg0 = Math.PI / 2;
        private static readonly double TopEdgeAngSeg2 = Math.PI * 3 / 2;

        private static Vector3 CalcTopEdgedSquare(double angle)
        {
            Vector3 c_p3;
            double a1;
            double b1;
            double d1;

            if (angle < TopEdgeAngSeg0)
            {
                a1 = TopEdgeEqu_A[0];
                b1 = TopEdgeEqu_B[0];
                d1 = TopEdgeEqu_D[0];
            }
            else if (angle <= Math.PI)
            {
                a1 = TopEdgeEqu_A[1];
                b1 = TopEdgeEqu_B[1];
                d1 = TopEdgeEqu_D[1];
            }
            else if (angle <= TopEdgeAngSeg2)
            {
                a1 = TopEdgeEqu_A[2];
                b1 = TopEdgeEqu_B[2];
                d1 = TopEdgeEqu_D[2];
            }
            else
            {
                a1 = TopEdgeEqu_A[3];
                b1 = TopEdgeEqu_B[3];
                d1 = TopEdgeEqu_D[3];
            }

            c_p3 = TOPEDGE_SQUARE_P0.Rotate2D_XY(angle);
            double a2 = c_p3.Y;
            double b2 = -c_p3.X;
            double d2 = a2 * c_p3.X + b2 * c_p3.Y;

            /* Cramer rule */
            double div = a1 * b2 - a2 * b1;
            double x = (b2 * d1 - b1 * d2) / div;
            double y = (a1 * d2 - a2 * d1) / div;
            return new Vector3(x, y, 0);
        }

        private static Vector3 CalcTrianglePoint(double angle)
        {
            /*                      p0 (0.5, 0.0)
             *                      /\
             *              side 1 /  \ side 3
             * (-0.5, -0.5)    p1 /____\  p2 (0.5, -0.5)
             *                    side 2
             */
            Vector3 c_p3;
            double a1;
            double b1;
            double d1;

            if(angle < 2.0943977032870302)
            {
                a1 = TriEqu_A[0];
                b1 = TriEqu_B[0];
                d1 = TriEqu_D[0];
            }
            else if(angle <= 2 * Math.PI - 2.0943977032870302)
            {
                a1 = TriEqu_A[1];
                b1 = TriEqu_B[1];
                d1 = TriEqu_D[1];
            }
            else
            {
                a1 = TriEqu_A[2];
                b1 = TriEqu_B[2];
                d1 = TriEqu_D[2];
            }

            c_p3 = TRIANGLE_P0.Rotate2D_XY(angle);
            double a2 = c_p3.Y;
            double b2 = -c_p3.X;
            double d2 = a2 * c_p3.X + b2 * c_p3.Y;

            /* Cramer rule */
            double div = a1 * b2 - a2 * b1;
            double x = (b2 * d1 - b1 * d2) / div;
            double y = (a1 * d2 - a2 * d1) / div;
            return new Vector3(x, y, 0);
        }

        private sealed class ProfileDetails
        {
            public List<Vector3> Vertices = new List<Vector3>();
            public bool IsOpenHollow;
        }

        private static Vector3 CalcPointToSquareBoundary(this Vector3 v, double scale)
        {
            double dirXabs = Math.Abs(v.X);
            double dirYabs = Math.Abs(v.Y);
            return v * (dirXabs > dirYabs ?
                0.5 * scale / dirXabs :
                0.5 * scale / dirYabs);
        }

        private static void InsertAngle(List<double> angles, double angle)
        {
            int c = angles.Count;
            int i;
            for (i = 0; i < c; ++i)
            {
                if (angles[i] > angle)
                {
                    break;
                }
            }
            if (i == c)
            {
                angles.Add(angle);
            }
            else
            {
                angles.Insert(i, angle);
            }
        }

        private static readonly double[] CornerAngles = new double[] { Math.PI / 2, Math.PI, Math.PI * 1.5 };
        private static readonly double[] PrismAngles = new double[] { Math.PI / 2, Math.PI, Math.PI * 1.5, 2.0943977032870302, 2 * Math.PI - 2.0943977032870302 };
        private static List<double> CalcBaseAngles(this ObjectPart.PrimitiveShape.Decoded shape, double startangle, double endangle, double stepangle)
        {
            var angles = new List<double>();
            double genangle;
            if (shape.IsHollow || shape.ShapeType != PrimitiveShapeType.Box)
            {
                for (genangle = startangle; genangle < endangle; genangle += stepangle)
                {
                    angles.Add(genangle);
                }
            }
            else
            {
                angles.Add(startangle);
                angles.Add(endangle);
            }

            if(shape.HoleShape == PrimitiveProfileHollowShape.Triangle && shape.IsHollow)
            {
                foreach (var angle in PrismAngles)
                {
                    if (startangle <= angle && endangle >= angle && !angles.Contains(angle))
                    {
                        InsertAngle(angles, angle);
                    }
                }
            }
            if (shape.ShapeType == PrimitiveShapeType.Prism)
            {
                foreach (var angle in PrismAngles)
                {
                    if (startangle <= angle && endangle >= angle && !angles.Contains(angle))
                    {
                        InsertAngle(angles, angle);
                    }
                }
            }
            else
            {
                foreach (var angle in CornerAngles)
                {
                    if (startangle <= angle && endangle >= angle && !angles.Contains(angle))
                    {
                        InsertAngle(angles, angle);
                    }
                }
            }
            return angles;
        }

        private static ProfileDetails CalcBoxProfile(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* Box has cut start at 0.5,-0.5 */
            var profile = new ProfileDetails();

            double startangle = 2 * Math.PI * shape.ProfileBegin;
            double endangle = 2 * Math.PI * shape.ProfileEnd;
            double stepangle = (endangle - startangle) / 60;
            List<double> angles = shape.CalcBaseAngles(startangle, endangle, stepangle);

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = START_VECTOR_BOX * 0.5;
                foreach(var angle in angles)
                {
                    Vector3 outerDirectionalVec = startPoint.Rotate2D_XY(angle);
                    Vector3 innerDirectionalVec = outerDirectionalVec;

                    /* outer normalize on single component to 0.5, simplifies algorithm */
                    outerDirectionalVec = outerDirectionalVec.CalcPointToSquareBoundary(1);

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                            innerDirectionalVec = CalcTrianglePoint(angle).Rotate2D_XY(-2.3561944901923448) * shape.ProfileHollow * 0.5;
                            break;

                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec *= shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Same:
                        case PrimitiveProfileHollowShape.Square:
                            /* inner normalize on single component to 0.5 * hollow */
                            innerDirectionalVec = innerDirectionalVec.CalcPointToSquareBoundary(shape.ProfileHollow);
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    /* inner path is reversed */
                    profile.Vertices.Add(outerDirectionalVec);
                    profile.Vertices.Insert(0, innerDirectionalVec);
                }
                /* no center point here, even though we can have path cut */
                profile.IsOpenHollow = shape.IsOpen;
            }
            else
            {
                /* no hollow, so it becomes simple */
                Vector3 startPoint = START_VECTOR_BOX * 0.5;
                foreach (var angle in angles)
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(angle);
                    /* normalize on single component to 0.5, simplifies algorithm */
                    directionalVec = directionalVec.CalcPointToSquareBoundary(1);
                    profile.Vertices.Add(directionalVec);
                }
                if (shape.IsOpen)
                {
                    profile.Vertices.Add(new Vector3(0, 0, 0));
                }
                else
                {
                    profile.Vertices.RemoveAt(profile.Vertices.Count - 1);
                }
            }

            return profile;
        }

        private static ProfileDetails CalcCylinderProfile(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* Cylinder has cut start at 0,0.5 */
            var profile = new ProfileDetails();

            double startangle = 2 * Math.PI * shape.ProfileBegin;
            double endangle = 2 * Math.PI * shape.ProfileEnd;
            double stepangle = (endangle - startangle) / 60;
            var angles = shape.CalcBaseAngles(startangle, endangle, stepangle);

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                foreach(var angle in angles)
                {
                    Vector3 outerDirectionalVec = startPoint.Rotate2D_XY(angle);
                    Vector3 innerDirectionalVec = outerDirectionalVec;

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                            innerDirectionalVec = CalcTrianglePoint(angle) * shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Same:
                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec *= shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Square:
                            /* inner normalize on single component to 0.5 * hollow */
                            innerDirectionalVec = CalcTopEdgedSquare(angle) * shape.ProfileHollow;
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    /* inner path is reversed */
                    profile.Vertices.Add(outerDirectionalVec);
                    profile.Vertices.Insert(0, innerDirectionalVec);
                }
                /* no center point here, even though we can have path cut */
                profile.IsOpenHollow = shape.IsOpen;
            }
            else
            {
                /* no hollow, so it becomes simple */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                foreach (var angle in angles)
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(angle);
                    profile.Vertices.Add(directionalVec);
                }
                if (shape.IsOpen)
                {
                    profile.Vertices.Add(new Vector3(0, 0, 0));
                }
                else
                {
                    profile.Vertices.RemoveAt(profile.Vertices.Count - 1);
                }
            }

            return profile;
        }

        private static ProfileDetails CalcPrismProfile(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* Prism has cut start at 0,0.5 */
            var profile = new ProfileDetails();

            double startangle = 2 * Math.PI * shape.ProfileBegin;
            double endangle = 2 * Math.PI * shape.ProfileEnd;
            double stepangle = (endangle - startangle) / 60;
            var angles = shape.CalcBaseAngles(startangle, endangle, stepangle);

            if (shape.IsHollow)
            {
                /* we calculate two points */
                double profileHollow = shape.ProfileHollow * 0.5;
                Vector3 startPoint = Vector3.UnitX * 0.5;
                foreach (var angle in angles)
                {
                    Vector3 outerDirectionalVec = CalcTrianglePoint(angle);
                    Vector3 innerDirectionalVec;

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                        case PrimitiveProfileHollowShape.Same:
                            innerDirectionalVec = outerDirectionalVec * profileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec = startPoint.Rotate2D_XY(angle) * profileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Square:
                            innerDirectionalVec = CalcTopEdgedSquare(angle) * profileHollow;
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    /* inner path is reversed */
                    profile.Vertices.Add(outerDirectionalVec);
                    profile.Vertices.Insert(0, innerDirectionalVec);
                }
                /* no center point here, even though we can have path cut */
                profile.IsOpenHollow = shape.IsOpen;
            }
            else
            {
                /* no hollow, so it becomes simple */
                foreach (var angle in angles)
                {
                    Vector3 directionalVec = CalcTrianglePoint(angle);
                    profile.Vertices.Add(directionalVec);
                }
                if (shape.IsOpen)
                {
                    profile.Vertices.Add(new Vector3(0, 0, 0));
                }
                else
                {
                    profile.Vertices.RemoveAt(profile.Vertices.Count - 1);
                }
            }

            return profile;
        }
        #endregion
    }
}
