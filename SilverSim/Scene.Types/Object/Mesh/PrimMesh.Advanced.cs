﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        #region extrude advanced
        static List<Vector3> ExtrudeAdvanced(this PathDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            List<Vector3> extrusionPath = new List<Vector3>();
            double twist = twistBegin.Lerp(twistEnd, cut);
            double angle = 0.0.Lerp(shape.Revolutions * 2 * Math.PI, cut);
            Vector3 topSize = new Vector3();
            Vector3 shear = new Vector3();
            Vector3 taper = new Vector3();
            double radiusOffset;

            #region cut
            if (shape.PathScale.X < 0f)
            {
                topSize.X = 1.0.Clamp(1f + shape.PathScale.X, 1f - cut);
            }
            else
            {
                topSize.X = 1.0.Clamp(1f - shape.PathScale.X, cut);
            }
            if (shape.PathScale.Y < 0f)
            {
                topSize.Y = 1.0.Clamp(1f + shape.PathScale.Y, 1f - cut);
            }
            else
            {
                topSize.Y = 1.0.Clamp(1f - shape.PathScale.Y, cut);
            }
            #endregion

            #region top_shear
            if (shape.TopShear.X < 0f)
            {
                shear.X = 1.0.Clamp(1f + shape.TopShear.X, 1f - cut);
            }
            else
            {
                shear.X = 1.0.Clamp(1f - shape.TopShear.X, cut);
            }
            if (shape.TopShear.Y < 0f)
            {
                shear.Y = 1.0.Clamp(1f + shape.TopShear.Y, 1f - cut);
            }
            else
            {
                shear.Y = 1.0.Clamp(1f - shape.TopShear.Y, 1f - cut);
            }
            #endregion

            #region taper
            if (shape.Taper.X < 0f)
            {
                taper.X = 1.0.Clamp(1f + shape.Taper.X, 1f - cut);
            }
            else
            {
                taper.X = 1.0.Clamp(1f - shape.Taper.X, cut);
            }
            if (shape.Taper.Y < 0f)
            {
                taper.Y = 1.0.Clamp(1f + shape.Taper.Y, 1f - cut);
            }
            else
            {
                taper.Y = 1.0.Clamp(1f - shape.Taper.Y, cut);
            }
            #endregion

            #region radius offset
            if(shape.RadiusOffset < 0f)
            {
                radiusOffset = 1.0.Clamp(1f + shape.RadiusOffset, 1f - cut);
            }
            else
            {
                radiusOffset = 1.0.Clamp(1f - shape.RadiusOffset, cut);
            }
            #endregion

            /* generate extrusions */
            foreach (Vector3 vertex in path.Vertices)
            {
                Vector3 outvertex = vertex;
                outvertex.X *= topSize.X;
                outvertex.Y *= topSize.Y;
                outvertex += shape.TopShear;
                outvertex.Z *= shape.PathScale.X;
                outvertex.Y *= taper.Y;
                outvertex.Z *= taper.Z;
                outvertex = outvertex.Rotate2D_YZ(twist);
                outvertex.Z *= (1 / (shape.Skew * shape.Revolutions));
                outvertex.Z += shape.Skew * shape.Revolutions * cut;
                outvertex.Y *= 0.5 * shape.PathScale.Y;
                outvertex.Y += 0.5 * (1 - shape.PathScale.Y);
                outvertex += outvertex.Rotate2D_XY(angle);
                outvertex.Z += outvertex.X * shape.TopShear.X;
                outvertex.Y += outvertex.X * shape.TopShear.Y;
                outvertex.X *= radiusOffset;
                outvertex.Y *= radiusOffset;

                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }
        #endregion

        #region advanced mesh generator
        static Mesh AdvancedToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path;
            switch (shape.ShapeType)
            {
                case PrimitiveShapeType.Ring:
                    path = CalcRingPath(shape);
                    break;

                case PrimitiveShapeType.Torus:
                    path = CalcTorusPath(shape);
                    break;

                case PrimitiveShapeType.Tube:
                    path = CalcTubePath(shape);
                    break;

                case PrimitiveShapeType.Sphere:
                    path = CalcSpherePath(shape);
                    break;

                default:
                    throw new NotImplementedException();
            }

            double cut = shape.PathBegin;
            double cutEnd = shape.PathEnd;
            double cutStep = (cutEnd - cut) / 36f / shape.Revolutions;
            double twistBegin = shape.TwistBegin * Math.PI * 2;
            double twistEnd = shape.TwistEnd * Math.PI * 2;

            Mesh mesh = new Mesh();
            if (shape.ShapeType == PrimitiveShapeType.Sphere)
            {
                for (; cut < cutEnd; cut += cutStep)
                {
                    mesh.Vertices.AddRange(path.ExtrudeSphere(shape, twistBegin, twistEnd, cut));
                }
                mesh.Vertices.AddRange(path.ExtrudeSphere(shape, twistBegin, twistEnd, cutEnd));
            }
            else
            {
                for (; cut < cutEnd; cut += cutStep)
                {
                    mesh.Vertices.AddRange(path.ExtrudeAdvanced(shape, twistBegin, twistEnd, cut));
                }
                mesh.Vertices.AddRange(path.ExtrudeAdvanced(shape, twistBegin, twistEnd, cutEnd));
            }

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
            if (shape.IsHollow)
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

                if (!path.IsOpenHollow)
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
                for (int l = 0; l < verticeRowCount; ++l)
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
        static PathDetails CalcTorusPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path = shape.CalcCylinderPath();
            int i;
            for(i = 0; i < path.Vertices.Count; ++i)
            {
                /* rotate vertices */
                Vector3 v = path.Vertices[i];
                path.Vertices[i] = new Vector3(0, v.Y, v.X);
            }

            return path;
        }

        static PathDetails CalcRingPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path = shape.CalcPrismPath();
            int i;
            for (i = 0; i < path.Vertices.Count; ++i)
            {
                /* rotate vertices */
                Vector3 v = path.Vertices[i];
                path.Vertices[i] = new Vector3(0, v.Y, v.X);
            }

            return path;
        }

        static PathDetails CalcTubePath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path = shape.CalcBoxPath();
            int i;
            for (i = 0; i < path.Vertices.Count; ++i)
            {
                /* rotate vertices */
                Vector3 v = path.Vertices[i];
                path.Vertices[i] = new Vector3(0, v.Y, v.X);
            }

            return path;
        }
        #endregion
    }
}
