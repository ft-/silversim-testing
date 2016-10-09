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
            for (; cut < cutEnd; cut += cutStep)
            {
                mesh.Vertices.AddRange(path.ExtrudeBasic(shape, twistBegin, twistEnd, cut));
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

                /*
                if (path.IsOpenHollow)
                {
                    Triangle tri = new Triangle();
                    tri.Vertex1 = 0;
                    tri.Vertex2 = verticeRowCount - 1;
                    tri.Vertex3 = verticeRowCount / 2;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = 0;
                    tri.Vertex2 = verticeRowCount / 2 - 1;
                    tri.Vertex3 = verticeRowCount / 2;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = bottomIndex;
                    tri.Vertex2 = verticeRowCount - 1 + bottomIndex;
                    tri.Vertex3 = verticeRowCount / 2 + bottomIndex;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = bottomIndex;
                    tri.Vertex2 = verticeRowCount / 2 - 1 + bottomIndex;
                    tri.Vertex3 = verticeRowCount / 2 + bottomIndex;
                    mesh.Triangles.Add(tri);
                }*/
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

        const double TRIANGLE_ANGLE_SECTIONS = 2f / 3f * Math.PI;
        static readonly Vector3 TRIANGLE_P0 = new Vector3(0.5, 0, 0);
        static readonly Vector3 TRIANGLE_P1 = new Vector3(0.5, -0.5, 0);
        static readonly Vector3 TRIANGLE_P2 = new Vector3(-0.5, -0.5, 0);

        static Vector3 CalcTrianglePoint(double angle)
        {
            /*                      p0 (0.5, 0.0)
             *                      /\
             *              side 3 /  \ side 1
             * (-0.5, -0.5)    p2 /____\  p1 (0.5, -0.5)
             *                    side 2
             */
            Vector3 c_p1;
            Vector3 c_p2;
            Vector3 c_p3;

            if(angle < TRIANGLE_ANGLE_SECTIONS)
            {
                c_p1 = TRIANGLE_P0;
                c_p2 = TRIANGLE_P1;
            }
            else if(angle < 2 * TRIANGLE_ANGLE_SECTIONS)
            {
                c_p1 = TRIANGLE_P1;
                c_p2 = TRIANGLE_P2;
            }
            else
            {
                c_p1 = TRIANGLE_P2;
                c_p2 = TRIANGLE_P0;
            }

            double above_div_common;
            double x_above_div;
            double below_div;
            double y_above_div;

            c_p3 = new Vector3(Math.Cos(angle), Math.Sin(angle), 0);

            /* According to Cramer, we can calculate from two lines the intersection point.
             * The original algorithm has four points (two per line) but our fourth is always the Zero Vector.
             * So, point 4 is eliminated.
             */
            above_div_common = -(c_p2.X * c_p1.Y - c_p1.X * c_p2.Y);
            x_above_div = c_p3.X * above_div_common;
            below_div = (-c_p3.Y) * (c_p2.X - c_p1.X) - (c_p2.Y - c_p1.Y) * (-c_p3.X);

            y_above_div = c_p3.Y * above_div_common;
            return new Vector3(x_above_div / below_div, y_above_div / below_div, 0);
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
        static List<double> CalcBaseAngles(double startangle, double endangle, double stepangle)
        {
            List<double> angles = new List<double>();
            double genangle;
            for (genangle = startangle; genangle < endangle; genangle += stepangle)
            {
                angles.Add(genangle);
            }

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
            return angles;
        }

        static PathDetails CalcBoxPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
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
            List<double> angles = CalcBaseAngles(startangle, endangle, stepangle);

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
                            innerDirectionalVec = CalcTrianglePoint(angle) * shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec *= (shape.ProfileHollow * 0.5);
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
            List<double> angles = CalcBaseAngles(startangle, endangle, stepangle);

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
                            innerDirectionalVec *= (shape.ProfileHollow * 0.5);
                            break;

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
            List<double> angles = CalcBaseAngles(startangle, endangle, stepangle);

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                foreach (double angle in angles)
                {
                    Vector3 outerDirectionalVec = CalcTrianglePoint(angle);
                    Vector3 innerDirectionalVec;

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                        case PrimitiveProfileHollowShape.Same:
                            innerDirectionalVec = outerDirectionalVec * shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec = startPoint.Rotate2D_XY(angle);
                            innerDirectionalVec *= (shape.ProfileHollow * 0.5);
                            break;

                        case PrimitiveProfileHollowShape.Square:
                            innerDirectionalVec = startPoint.Rotate2D_XY(angle);
                            double innerDirXabs = Math.Abs(innerDirectionalVec.X);
                            double innerDirYabs = Math.Abs(innerDirectionalVec.Y);
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
                Vector3 startPoint = Vector3.UnitX * 0.5;
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
