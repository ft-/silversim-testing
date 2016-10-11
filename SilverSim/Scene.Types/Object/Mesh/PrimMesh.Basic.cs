// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        static void CalcTopSizeAndShear(ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut, out Vector3 topSize, out Vector3 shear, out double twist)
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
                0.0.Lerp(shape.TopShear.X, 1f - cut) :
                0.0.Lerp(shape.TopShear.X, cut);

            shear.Y = shape.TopShear.Y < 0f ?
                0.0.Lerp(shape.TopShear.Y, 1f - cut) :
                0.0.Lerp(shape.TopShear.Y, cut);
            #endregion
        }

        static Vector3 ApplyTortureParams(ObjectPart.PrimitiveShape.Decoded shape, Vector3 v, double twistBegin, double twistEnd, double cut)
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
            return outvertex;
        }

        static List<Vector3> ExtrudeBasic(this PathDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            List<Vector3> extrusionPath = new List<Vector3>();
            double twist;
            Vector3 topSize;
            Vector3 shear;
            CalcTopSizeAndShear(shape, twistBegin, twistEnd, cut, out topSize, out shear, out twist);

            /* generate extrusions */
            foreach (Vector3 vertex in path.Vertices)
            {
                Vector3 outvertex = vertex;
                outvertex.X *= topSize.X;
                outvertex.Y *= topSize.Y;
                outvertex = outvertex.Rotate2D_XY(twist);
                outvertex += shear;
                outvertex.Z = cut - 0.5;
                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }

        static MeshLOD BoxCylPrismToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Box:
                    path = CalcBoxPath(shape);
                    break;

                case PrimitiveShapeType.Cylinder:
                    path = CalcCylinderPath(shape);
                    break;

                case PrimitiveShapeType.Prism:
                    path = CalcPrismPath(shape);
                    break;

                default:
                    throw new NotImplementedException();
            }

            double cut = shape.ProfileBegin;
            double cutBegin = cut;
            double cutEnd = 1f - shape.ProfileEnd;
            double cutStep = (cutEnd - cut) / 10f;
            double twistBegin = shape.TwistBegin * Math.PI;
            double twistEnd = shape.TwistEnd * Math.PI;

            MeshLOD mesh = new MeshLOD();
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

            int verticeRowCount = path.Vertices.Count;
            int verticeTotalCount = mesh.Vertices.Count;
            int verticeRowEndCount = verticeRowCount;
            if(!shape.IsOpen && shape.IsHollow)
            {
                verticeRowEndCount -= 1;
            }

            /* generate z-triangles */
            for (int l = 0; l < verticeRowEndCount; ++l)
            {
                if(!shape.IsOpen && shape.IsHollow && l == verticeRowCount / 2 - 1)
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
                    Triangle tri = new Triangle();
                    tri.Vertex1 = z + l;
                    tri.Vertex2 = z2 + l2;
                    tri.Vertex3 = z + l2;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = z + l;
                    tri.Vertex2 = z2 + l;
                    tri.Vertex3 = z2 + l2;
                    mesh.Triangles.Add(tri);
                }
            }

            /* generate top and bottom triangles */
            if(shape.IsHollow)
            {
                /* simpler just close neighboring dots */
                /* no need for uneven check here.
                 * The path generator always generates two pathes here which are connected and therefore always a multiple of two */
                int bottomIndex = verticeTotalCount - verticeRowCount;
                for (int l = 0; l < verticeRowCount / 2; ++l)
                {
                    int l2 = verticeRowCount - l - 1;

                    Triangle tri = new Triangle();
                    tri.Vertex1 = l;
                    tri.Vertex2 = l2;
                    tri.Vertex3 = l + 1;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = l + 1;
                    tri.Vertex2 = l2;
                    tri.Vertex3 = l2 - 1;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = l + bottomIndex;
                    tri.Vertex2 = l2 + bottomIndex;
                    tri.Vertex3 = l + 1 + bottomIndex;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = l + 1 + bottomIndex;
                    tri.Vertex2 = l2 + bottomIndex;
                    tri.Vertex3 = l2 - 1 + bottomIndex;
                    mesh.Triangles.Add(tri);
                }
            }
            else
            {
                /* build a center point and connect all vertexes with triangles */
                double z1;
                double z2;

                z1 = mesh.Vertices[0].Z;
                z2 = mesh.Vertices[verticeTotalCount - 1].Z;
                int centerpointTop = mesh.Vertices.Count;
                int bottomIndex = verticeTotalCount - verticeRowCount;
                mesh.Vertices.Add(ApplyTortureParams(shape, new Vector3(0, 0, 0), twistBegin, twistEnd, cutBegin));
                int centerpointBottom = mesh.Vertices.Count;
                mesh.Vertices.Add(ApplyTortureParams(shape, new Vector3(0, 0, 0), twistBegin, twistEnd, cutEnd));
                for(int l = 0; l < verticeRowCount; ++l)
                {
                    int l2 = (l + 1) % verticeRowCount;

                    Triangle tri = new Triangle();
                    tri.Vertex1 = l;
                    tri.Vertex2 = l2;
                    tri.Vertex3 = centerpointTop;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = l + bottomIndex;
                    tri.Vertex2 = l2 + bottomIndex;
                    tri.Vertex3 = centerpointBottom;
                    mesh.Triangles.Add(tri);
                }
            }

            return mesh;
        }
        #endregion

        #region 2D Path calculation
        static Vector3 Rotate2D_XY(this Vector3 vec, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            return new Vector3(
                vec.X * cos - vec.Y * sin,
                vec.X * sin + vec.Y * cos,
                0);
        }

        static Vector3 Rotate2D_YZ(this Vector3 vec, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            return new Vector3(
                0,
                vec.Y * cos - vec.Z * sin,
                vec.Y * sin + vec.Z * cos);
        }

        static double CalcEquA(Vector3 v1, Vector3 v2)
        {
            return v1.Y - v2.Y;
        }

        static double CalcEquB(Vector3 v1, Vector3 v2)
        {
            return v2.X - v1.X;
        }
        
        static double CalcEquD(Vector3 v1, Vector3 v2)
        {
            return CalcEquA(v1, v2) * v1.X + CalcEquB(v1, v2) * v1.Y;
        }

        static double CalcTriBaseAngle(Vector3 v)
        {
            double ang = Math.Atan2(v.Y, v.X) - Math.Atan2(0.5, 0);
            while(ang < 0)
            {
                ang += 2 * Math.PI;
            }
            return ang;
        }

        static readonly Vector3 TRIANGLE_P0 = Vector3.UnitX * 0.5;
        static readonly Vector3 TRIANGLE_P1 = Vector3.UnitX.Rotate2D_XY(2.0943977032870302) * 0.5;
        static readonly Vector3 TRIANGLE_P2 = Vector3.UnitX.Rotate2D_XY(-2.0943977032870302) * 0.5;
        static readonly double[] TriEqu_A = new double[3] { CalcEquA(TRIANGLE_P0, TRIANGLE_P1), CalcEquA(TRIANGLE_P1, TRIANGLE_P2), CalcEquA(TRIANGLE_P2, TRIANGLE_P0) };
        static readonly double[] TriEqu_B = new double[3] { CalcEquB(TRIANGLE_P0, TRIANGLE_P1), CalcEquB(TRIANGLE_P1, TRIANGLE_P2), CalcEquB(TRIANGLE_P2, TRIANGLE_P0) };
        static readonly double[] TriEqu_D = new double[3] { CalcEquD(TRIANGLE_P0, TRIANGLE_P1), CalcEquD(TRIANGLE_P1, TRIANGLE_P2), CalcEquD(TRIANGLE_P2, TRIANGLE_P0) };

        static readonly Vector3 TOPEDGE_SQUARE_P0 = Vector3.UnitX * 0.5 / 0.7;
        static readonly Vector3 TOPEDGE_SQUARE_P1 = Vector3.UnitY * 0.5 / 0.7;
        static readonly Vector3 TOPEDGE_SQUARE_P2 = -Vector3.UnitX * 0.5 / 0.7;
        static readonly Vector3 TOPEDGE_SQUARE_P3 = -Vector3.UnitY * 0.5 / 0.7;

        static readonly double[] TopEdgeEqu_A = new double[4] { CalcEquA(TOPEDGE_SQUARE_P0, TOPEDGE_SQUARE_P1), CalcEquA(TOPEDGE_SQUARE_P1, TOPEDGE_SQUARE_P2), CalcEquA(TOPEDGE_SQUARE_P2, TOPEDGE_SQUARE_P3), CalcEquA(TOPEDGE_SQUARE_P3, TOPEDGE_SQUARE_P0) };
        static readonly double[] TopEdgeEqu_B = new double[4] { CalcEquB(TOPEDGE_SQUARE_P0, TOPEDGE_SQUARE_P1), CalcEquB(TOPEDGE_SQUARE_P1, TOPEDGE_SQUARE_P2), CalcEquB(TOPEDGE_SQUARE_P2, TOPEDGE_SQUARE_P3), CalcEquB(TOPEDGE_SQUARE_P3, TOPEDGE_SQUARE_P0) };
        static readonly double[] TopEdgeEqu_D = new double[4] { CalcEquD(TOPEDGE_SQUARE_P0, TOPEDGE_SQUARE_P1), CalcEquD(TOPEDGE_SQUARE_P1, TOPEDGE_SQUARE_P2), CalcEquD(TOPEDGE_SQUARE_P2, TOPEDGE_SQUARE_P3), CalcEquD(TOPEDGE_SQUARE_P3, TOPEDGE_SQUARE_P0) };
        static readonly double TopEdgeAngSeg0 = Math.PI / 2;
        static readonly double TopEdgeAngSeg2 = Math.PI * 3 / 2;

        static Vector3 CalcTopEdgedSquare(double angle)
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
            double div = (a1 * b2 - a2 * b1);
            double x = (b2 * d1 - b1 * d2) / div;
            double y = (a1 * d2 - a2 * d1) / div;
            return new Vector3(x, y, 0);
        }

        static Vector3 CalcTrianglePoint(double angle)
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
            double div = (a1 * b2 - a2 * b1);
            double x = (b2 * d1 - b1 * d2) / div;
            double y = (a1 * d2 - a2 * d1) / div;
            return new Vector3(x, y, 0);
        }

        sealed class PathDetails
        {
            public List<Vector3> Vertices = new List<Vector3>();
            public bool IsOpenHollow;

            public PathDetails()
            {

            }
        }

        static Vector3 CalcPointToSquareBoundary(this Vector3 v, double scale)
        {
            double dirXabs = Math.Abs(v.X);
            double dirYabs = Math.Abs(v.Y);
            return v * (dirXabs > dirYabs ?
                0.5 * scale / dirXabs :
                0.5 * scale / dirYabs);
        }

        static void InsertAngle(List<double> angles, double angle)
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

        static readonly double[] CornerAngles = new double[] { Math.PI / 2, Math.PI, Math.PI * 1.5 };
        static readonly double[] PrismAngles = new double[] { Math.PI / 2, Math.PI, Math.PI * 1.5, 2.0943977032870302, 2 * Math.PI - 2.0943977032870302 };
        static List<double> CalcBaseAngles(this ObjectPart.PrimitiveShape.Decoded shape, double startangle, double endangle, double stepangle)
        {
            List<double> angles = new List<double>();
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
                foreach (double angle in PrismAngles)
                {
                    if (startangle <= angle && endangle >= angle)
                    {
                        if (!angles.Contains(angle))
                        {
                            InsertAngle(angles, angle);
                        }
                    }
                }

            }
            if (shape.ShapeType == PrimitiveShapeType.Prism)
            {
                foreach (double angle in PrismAngles)
                {
                    if (startangle <= angle && endangle >= angle)
                    {
                        if (!angles.Contains(angle))
                        {
                            InsertAngle(angles, angle);
                        }
                    }
                }
            }
            else
            {
                foreach (double angle in CornerAngles)
                {
                    if (startangle <= angle && endangle >= angle)
                    {
                        if (!angles.Contains(angle))
                        {
                            InsertAngle(angles, angle);
                        }
                    }
                }
            }
            return angles;
        }

        static PathDetails CalcBoxPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* Box has cut start at 0.5,-0.5 */
            PathDetails Path = new PathDetails();

            double startangle;
            double endangle;
            switch (shape.ShapeType)
            {
                case PrimitiveShapeType.Ring:
                case PrimitiveShapeType.Torus:
                case PrimitiveShapeType.Tube:
                    startangle = 2 * Math.PI * shape.ProfileBegin;
                    endangle = 2 * Math.PI * (1f - shape.ProfileEnd);
                    break;

                default:
                    startangle = 2 * Math.PI * shape.PathBegin;
                    endangle = 2 * Math.PI * shape.PathEnd;
                    break;
            }
            double stepangle = (endangle - startangle) / 60;
            List<double> angles = shape.CalcBaseAngles(startangle, endangle, stepangle);

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = START_VECTOR_BOX * 0.5;
                foreach(double angle in angles)
                {
                    Vector3 outerDirectionalVec = startPoint.Rotate2D_XY(angle);
                    Vector3 innerDirectionalVec = outerDirectionalVec;

                    double outerDirXabs = Math.Abs(outerDirectionalVec.X);
                    double outerDirYabs = Math.Abs(outerDirectionalVec.Y);

                    /* outer normalize on single component to 0.5, simplifies algorithm */
                    outerDirectionalVec = outerDirectionalVec.CalcPointToSquareBoundary(1);

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                            innerDirectionalVec = CalcTrianglePoint(angle).Rotate2D_XY(-2.3561944901923448) * shape.ProfileHollow * 0.5;
                            break;

                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec *= (shape.ProfileHollow);
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
                    Path.Vertices.Add(outerDirectionalVec);
                    Path.Vertices.Insert(0, innerDirectionalVec);
                }
                /* no center point here, even though we can have path cut */
                Path.IsOpenHollow = shape.IsOpen;
            }
            else
            {
                /* no hollow, so it becomes simple */
                Vector3 startPoint = START_VECTOR_BOX * 0.5;
                foreach (double angle in angles)
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(angle);
                    /* normalize on single component to 0.5, simplifies algorithm */
                    directionalVec = directionalVec.CalcPointToSquareBoundary(1);
                    Path.Vertices.Add(directionalVec);
                }
                if (shape.IsOpen)
                {
                    Path.Vertices.Add(new Vector3(0, 0, 0));
                }
            }

            return Path;
        }

        static PathDetails CalcCylinderPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* Cylinder has cut start at 0,0.5 */
            PathDetails Path = new PathDetails();

            double startangle;
            double endangle;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Ring:
                case PrimitiveShapeType.Torus:
                case PrimitiveShapeType.Tube:
                    startangle = 2 * Math.PI * shape.ProfileBegin;
                    endangle = 2 * Math.PI * (1f - shape.ProfileEnd);
                    break;

                default:
                    startangle = 2 * Math.PI * shape.PathBegin;
                    endangle = 2 * Math.PI * shape.PathEnd;
                    break;
            }
            double stepangle = (endangle - startangle) / 60;
            List<double> angles = shape.CalcBaseAngles(startangle, endangle, stepangle);

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                foreach(double angle in angles)
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
                            innerDirectionalVec *= (shape.ProfileHollow);
                            break;

                        case PrimitiveProfileHollowShape.Square:
                            /* inner normalize on single component to 0.5 * hollow */
                            innerDirectionalVec = CalcTopEdgedSquare(angle) * shape.ProfileHollow;
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    /* inner path is reversed */
                    Path.Vertices.Add(outerDirectionalVec);
                    Path.Vertices.Insert(0, innerDirectionalVec);

                }
                /* no center point here, even though we can have path cut */
                Path.IsOpenHollow = shape.IsOpen;
            }
            else
            {
                /* no hollow, so it becomes simple */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                foreach (double angle in angles)
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(angle);
                    Path.Vertices.Add(directionalVec);
                }
                if (shape.IsOpen)
                {
                    Path.Vertices.Add(new Vector3(0, 0, 0));
                }
            }

            return Path;
        }

        static PathDetails CalcPrismPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* Prism has cut start at 0,0.5 */
            PathDetails Path = new PathDetails();

            double startangle;
            double endangle;
            switch (shape.ShapeType)
            {
                case PrimitiveShapeType.Ring:
                case PrimitiveShapeType.Torus:
                case PrimitiveShapeType.Tube:
                    startangle = 2 * Math.PI * shape.ProfileBegin;
                    endangle = 2 * Math.PI * (1f - shape.ProfileEnd);
                    break;

                default:
                    startangle = 2 * Math.PI * shape.PathBegin;
                    endangle = 2 * Math.PI * shape.PathEnd;
                    break;
            }
            double stepangle = (endangle - startangle) / 60;
            List<double> angles = shape.CalcBaseAngles(startangle, endangle, stepangle);

            if (shape.IsHollow)
            {
                /* we calculate two points */
                double profileHollow = shape.ProfileHollow * 0.5;
                Vector3 startPoint = Vector3.UnitX * 0.5;
                foreach (double angle in angles)
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
                    Path.Vertices.Add(outerDirectionalVec);
                    Path.Vertices.Insert(0, innerDirectionalVec);

                }
                /* no center point here, even though we can have path cut */
                Path.IsOpenHollow = shape.IsOpen;
            }
            else
            {
                /* no hollow, so it becomes simple */
                foreach (double angle in angles)
                {
                    Vector3 directionalVec = CalcTrianglePoint(angle);
                    Path.Vertices.Add(directionalVec);
                }
                if (shape.IsOpen)
                {
                    Path.Vertices.Add(new Vector3(0, 0, 0));
                }
            }

            return Path;
        }
        #endregion
    }
}
