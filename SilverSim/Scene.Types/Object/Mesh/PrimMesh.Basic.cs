// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        #region Box/Cylinder/Prism calculation
        static List<Vector3> ExtrudeBasic(this PathDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            List<Vector3> extrusionPath = new List<Vector3>();
            double twist = twistBegin.Lerp(twistEnd, cut);
            Vector3 topSize = new Vector3();
            Vector3 shear = new Vector3();

            #region cut
            topSize.X = shape.PathScale.X < 0f ?
                1.0.Clamp(1f + shape.PathScale.X, 1f - cut) :
                1.0.Clamp(1f - shape.PathScale.X, cut);

            topSize.Y = shape.PathScale.Y < 0f ?
                1.0.Clamp(1f + shape.PathScale.Y, 1f - cut) :
                1.0.Clamp(1f - shape.PathScale.Y, cut);
            #endregion

            #region top_shear
            shear.X = shape.TopShear.X < 0f ?
                1.0.Clamp(1f + shape.TopShear.X, 1f - cut) :
                1.0.Clamp(1f - shape.TopShear.X, cut);
            
            shear.Y = shape.TopShear.Y < 0f ?
                1.0.Clamp(1f + shape.TopShear.Y, 1f - cut) :
                1.0.Clamp(1f - shape.TopShear.Y, 1f - cut);
            #endregion

            /* generate extrusions */
            foreach (Vector3 vertex in path.Vertices)
            {
                Vector3 outvertex = vertex;
                outvertex.X *= topSize.X;
                outvertex.Y *= topSize.Y;
                outvertex += shape.TopShear;
                outvertex = outvertex.Rotate2D_XY(twist);
                outvertex.Z = cut - 0.5;
                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }

        static Mesh BoxCylPrismToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
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
            double cutEnd = 1f - shape.ProfileEnd;
            double cutStep = (cutEnd - cut) / 10f;
            double twistBegin = shape.TwistBegin * Math.PI;
            double twistEnd = shape.TwistEnd * Math.PI;

            Mesh mesh = new Mesh();
            for (; cut < cutEnd; cut += cutStep)
            {
                mesh.Vertices.AddRange(path.ExtrudeBasic(shape, twistBegin, twistEnd, cut));
            }
            mesh.Vertices.AddRange(path.ExtrudeBasic(shape, twistBegin, twistEnd, cutEnd));

            int verticeRowCount = path.Vertices.Count;
            int verticeTotalCount = mesh.Vertices.Count;

            /* generate z-triangles */
            for (int z = 0; z < verticeTotalCount - verticeRowCount; z += verticeRowCount)
            {
                for (int l = 0; l < verticeRowCount; ++l)
                {
                    /* p0  ___  p1 */
                    /*    |   |    */
                    /*    |___|    */
                    /* p3       p2 */
                    /* tris: p0, p1, p2 and p0, p3, p2 */
                    /* p2 and p3 are on next row */
                    int z2 = (z + 1) % verticeTotalCount;
                    int l2 = (l + 1) % verticeRowCount; /* loop closure */
                    Mesh.Triangle tri = new Mesh.Triangle();
                    tri.VectorIndex0 = z + l;
                    tri.VectorIndex1 = z + l + 1;
                    tri.VectorIndex2 = z2 + l2 + 1;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = z + l;
                    tri.VectorIndex1 = z2 + l;
                    tri.VectorIndex2 = z2 + l2 + 1;
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

                    Mesh.Triangle tri = new Mesh.Triangle();
                    tri.VectorIndex0 = l;
                    tri.VectorIndex1 = l + 1;
                    tri.VectorIndex2 = l2;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = l + 1;
                    tri.VectorIndex1 = l2;
                    tri.VectorIndex2 = l2 - 1;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = l + 1 + bottomIndex;
                    tri.VectorIndex1 = l2 + bottomIndex;
                    tri.VectorIndex2 = l2 - 1 + bottomIndex;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = l + 1 + bottomIndex;
                    tri.VectorIndex1 = l2 + bottomIndex;
                    tri.VectorIndex2 = l2 - 1 + bottomIndex;
                    mesh.Triangles.Add(tri);
                }

                if(!path.IsOpenHollow)
                {
                    Mesh.Triangle tri = new Mesh.Triangle();
                    tri.VectorIndex0 = 0;
                    tri.VectorIndex1 = verticeRowCount - 1;
                    tri.VectorIndex2 = verticeRowCount / 2;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = 0;
                    tri.VectorIndex1 = verticeRowCount / 2 - 1;
                    tri.VectorIndex2 = verticeRowCount / 2;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = bottomIndex;
                    tri.VectorIndex1 = verticeRowCount - 1 + bottomIndex;
                    tri.VectorIndex2 = verticeRowCount / 2 + bottomIndex;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = bottomIndex;
                    tri.VectorIndex1 = verticeRowCount / 2 - 1 + bottomIndex;
                    tri.VectorIndex2 = verticeRowCount / 2 + bottomIndex;
                    mesh.Triangles.Add(tri);
                }
            }
            else
            {
                /* build a center point and connect all vertexes with triangles */
                double z1, z2;
                z1 = mesh.Vertices[0].Z;
                z2 = mesh.Vertices[verticeTotalCount - 1].Z;
                int centerpointTop = mesh.Vertices.Count;
                int bottomIndex = verticeTotalCount - verticeRowCount;
                mesh.Vertices.Add(new Vector3(0, 0, z1));
                int centerpointBottom = mesh.Vertices.Count;
                mesh.Vertices.Add(new Vector3(0, 0, z2));
                for(int l = 0; l < verticeRowCount; ++l)
                {
                    int l2 = (l + 1) % verticeRowCount;

                    Mesh.Triangle tri = new Mesh.Triangle();
                    tri.VectorIndex0 = l;
                    tri.VectorIndex1 = l2;
                    tri.VectorIndex2 = centerpointTop;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = l + bottomIndex;
                    tri.VectorIndex1 = l2 + bottomIndex;
                    tri.VectorIndex2 = centerpointBottom;
                    mesh.Triangles.Add(tri);
                }
            }

            return mesh;
        }
        #endregion

        #region 2D Path calculation
        static Vector3 Rotate2D_XY(this Vector3 vec, double angle)
        {
            return new Vector3(
                vec.Y * Math.Sin(angle) + vec.X * Math.Cos(angle),
                vec.X * Math.Sin(angle) + vec.Y * Math.Cos(angle),
                0);
        }

        static Vector3 Rotate2D_YZ(this Vector3 vec, double angle)
        {
            return new Vector3(
                0,
                vec.Z * Math.Sin(angle) + vec.Y * Math.Cos(angle),
                vec.Y * Math.Sin(angle) + vec.Z * Math.Cos(angle));
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

            if(shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = START_VECTOR_BOX * 0.5;
                for (; startangle < endangle; startangle += stepangle)
                {
                    Vector3 outerDirectionalVec = startPoint.Rotate2D_XY(startangle);
                    Vector3 innerDirectionalVec = outerDirectionalVec;

                    /* outer normalize on single component to 0.5, simplifies algorithm */
                    outerDirectionalVec *= Math.Abs(outerDirectionalVec.X) > Math.Abs(outerDirectionalVec.Y) ?
                        0.5 / outerDirectionalVec.X :
                        0.5 / outerDirectionalVec.Y;

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                            innerDirectionalVec = CalcTrianglePoint(shape.ProfileHollow);
                            break;

                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec *= (shape.ProfileHollow * 0.5);
                            break;

                        case PrimitiveProfileHollowShape.Same:
                        case PrimitiveProfileHollowShape.Square:
                            /* inner normalize on single component to 0.5 * hollow */
                            innerDirectionalVec *= Math.Abs(innerDirectionalVec.X) > Math.Abs(innerDirectionalVec.Y) ?
                                0.5 * shape.ProfileHollow / innerDirectionalVec.X :
                                0.5 * shape.ProfileHollow / innerDirectionalVec.Y;
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
                for (; startangle < endangle; startangle += stepangle )
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(startangle);
                    /* normalize on single component to 0.5, simplifies algorithm */
                    directionalVec *= Math.Abs(directionalVec.X) > Math.Abs(directionalVec.Y) ?
                        0.5 / directionalVec.X :
                        0.5 / directionalVec.Y;
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

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                for (; startangle < endangle; startangle += stepangle)
                {
                    Vector3 outerDirectionalVec = startPoint.Rotate2D_XY(startangle);
                    Vector3 innerDirectionalVec = outerDirectionalVec;

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                            innerDirectionalVec = CalcTrianglePoint(startangle) * shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Same:
                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec *= (shape.ProfileHollow * 0.5);
                            break;

                        case PrimitiveProfileHollowShape.Square:
                            /* inner normalize on single component to 0.5 * hollow */
                            innerDirectionalVec *= Math.Abs(innerDirectionalVec.X) > Math.Abs(innerDirectionalVec.Y) ?
                                0.5 * shape.ProfileHollow / innerDirectionalVec.X :
                                0.5 * shape.ProfileHollow / innerDirectionalVec.Y;
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
                for (; startangle < endangle; startangle += stepangle)
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(startangle);
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

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                for (; startangle < endangle; startangle += stepangle)
                {
                    Vector3 outerDirectionalVec = CalcTrianglePoint(startangle);
                    Vector3 innerDirectionalVec;

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                        case PrimitiveProfileHollowShape.Same:
                            innerDirectionalVec = outerDirectionalVec * shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec = startPoint.Rotate2D_XY(startangle);
                            innerDirectionalVec *= (shape.ProfileHollow * 0.5);
                            break;

                        case PrimitiveProfileHollowShape.Square:
                            innerDirectionalVec = startPoint.Rotate2D_XY(startangle);
                            /* inner normalize on single component to 0.5 * hollow */
                            innerDirectionalVec *= Math.Abs(innerDirectionalVec.X) > Math.Abs(innerDirectionalVec.Y) ?
                                0.5 * shape.ProfileHollow / innerDirectionalVec.X :
                                0.5 * shape.ProfileHollow / innerDirectionalVec.Y;
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
                for (; startangle < endangle; startangle += stepangle)
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(startangle);
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
