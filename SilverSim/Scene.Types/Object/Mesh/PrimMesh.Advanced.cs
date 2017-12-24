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
        #region extrude advanced
        private static List<Vector3> ExtrudeAdvanced(this PathDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            var extrusionPath = new List<Vector3>();
            double twist;
            double angle = 0.0.Lerp(shape.Revolutions * 2 * Math.PI, cut);
            var taper = new Vector3();
            double radiusOffset;

            twist = twistBegin.Lerp(twistEnd, cut);

            #region taper
            taper.X = shape.Taper.X < 0f ?
                1.0.Lerp(1 + shape.Taper.X, 1f - cut) :
                1.0.Lerp(1 - shape.Taper.X, cut);

            taper.Y = shape.Taper.Y < 0f ?
                1.0.Lerp(1 + shape.Taper.Y, 1f - cut) :
                1.0.Lerp(1 - shape.Taper.Y, cut);
            #endregion

            #region radius offset
            radiusOffset = shape.RadiusOffset > 0f ?
                1.0.Lerp(1 - shape.RadiusOffset, 1f - cut) :
                1.0.Lerp(1 + shape.RadiusOffset, cut);
            #endregion

            /* generate extrusions */
            double pathscale = shape.PathScale.Y;
            double skew = shape.Skew;
            foreach (var vertex in path.Vertices)
            {
                var outvertex = vertex;
                outvertex.X *= taper.X;
                outvertex.Y *= taper.Y;
                outvertex = outvertex.Rotate2D_XY(twist);

                outvertex.Z *= shape.PathScale.X;
                outvertex.Y *= 1 - Math.Abs(skew);
                outvertex.Y += skew * (1f - cut);

                outvertex.Y = outvertex.Y * pathscale + (0.5 - pathscale * 0.5) * radiusOffset;

                outvertex = outvertex.Rotate2D_YZ(-angle);
                outvertex.X += outvertex.Z * shape.TopShear.X;
                outvertex.Y += outvertex.Z * shape.TopShear.Y;

                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }

        private static Vector3 CalcAdvancedCenterPrim(this ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            double twist;
            double angle = 0.0.Lerp(shape.Revolutions * 2 * Math.PI, cut);
            var taper = new Vector3();
            double radiusOffset;

            twist = twistBegin.Lerp(twistEnd, cut);

            #region taper
            taper.X = shape.Taper.X < 0f ?
                1.0.Lerp(1 + shape.Taper.X, 1f - cut) :
                1.0.Lerp(1 - shape.Taper.X, cut);

            taper.Y = shape.Taper.Y < 0f ?
                1.0.Lerp(1 + shape.Taper.Y, 1f - cut) :
                1.0.Lerp(1 - shape.Taper.Y, cut);
            #endregion

            #region radius offset
            radiusOffset = shape.RadiusOffset > 0f ?
                1.0.Lerp(1 - shape.RadiusOffset, 1f - cut) :
                1.0.Lerp(1 + shape.RadiusOffset, cut);
            #endregion

            /* generate extrusions */
            double pathscale = shape.PathScale.Y;
            double skew = shape.Skew;
            var outvertex = Vector3.Zero;
            outvertex.Z *= taper.X;
            outvertex.Y *= taper.Y;
            outvertex = outvertex.Rotate2D_YZ(twist);

            outvertex.Z *= shape.PathScale.X;
            outvertex.Z *= 1 - Math.Abs(skew);
            outvertex.Z += skew * (1f - cut);

            outvertex.Y = outvertex.Y * pathscale + (0.5 - pathscale * 0.5) * radiusOffset;

            outvertex = outvertex.Rotate2D_XY(-angle);
            outvertex.Z += outvertex.X * shape.TopShear.X;
            outvertex.Y += outvertex.X * shape.TopShear.Y;

            return outvertex;
        }
        #endregion

        #region advanced mesh generator
        private static MeshLOD AdvancedToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path;
            switch (shape.ShapeType)
            {
                case PrimitiveShapeType.Ring:
                    path = CalcPrismPath(shape);
                    break;

                case PrimitiveShapeType.Torus:
                    path = CalcCylinderPath(shape);
                    break;

                case PrimitiveShapeType.Tube:
                    path = CalcBoxPath(shape);
                    break;

                case PrimitiveShapeType.Sphere:
                    path = CalcSpherePath(shape);
                    break;

                default:
                    throw new NotImplementedException();
            }

            double cut = shape.PathBegin;
            double cutBegin = cut;
            double cutEnd = shape.PathEnd;
            double cutStep = (cutEnd - cut) / 36f / shape.Revolutions;
            double twistBegin = shape.TwistBegin * Math.PI * 2;
            double twistEnd = shape.TwistEnd * Math.PI * 2;
            double neededSteps = Math.Max(1, Math.Ceiling((shape.TwistEnd - shape.TwistBegin) / (5 * Math.PI / 180) * (cutEnd - cut)));
            cutStep /= neededSteps;

            var mesh = new MeshLOD();
            for (; cut < cutEnd; cut += cutStep)
            {
                mesh.Vertices.AddRange(path.ExtrudeAdvanced(shape, twistBegin, twistEnd, cut));
            }
            mesh.Vertices.AddRange(path.ExtrudeAdvanced(shape, twistBegin, twistEnd, cutEnd));

            shape.BuildAdvancedTriangles(mesh, path, cutBegin, cutEnd);

            return mesh;
        }

        private static void BuildAdvancedTriangles(this ObjectPart.PrimitiveShape.Decoded shape, MeshLOD mesh, PathDetails path, double cutBegin, double cutEnd)
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
            else if(shape.ProfileBegin != shape.ProfileEnd)
            {
                /* build a center point and connect all vertices with triangles */
                int centerpointTop = mesh.Vertices.Count;
                int bottomIndex = verticeTotalCount - verticeRowCount;
                mesh.Vertices.Add(shape.CalcAdvancedCenterPrim(twistBegin, twistEnd, cutBegin));
                int centerpointBottom = mesh.Vertices.Count;
                mesh.Vertices.Add(shape.CalcAdvancedCenterPrim(twistBegin, twistEnd, cutEnd));
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
    }
}
