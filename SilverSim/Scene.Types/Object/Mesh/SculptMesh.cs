// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using CSJ2K;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static class SculptMesh
    {
        internal static Mesh SculptMeshToMesh(this AssetData data, ObjectPart.PrimitiveShape.Decoded shape)
        {
            if(data.Type != AssetType.Texture)
            {
                throw new InvalidSculptMeshAssetException();
            }
            using (Stream s = data.InputStream)
            {
                return s.SculptMeshToMesh(shape);
            }
        }

        internal static Mesh SculptMeshToMesh(this Stream st, ObjectPart.PrimitiveShape.Decoded shape)
        {
            using (Image im = J2kImage.FromStream(st))
            {
                Bitmap bitmap = new Bitmap(im);
                return bitmap.SculptMeshToMesh(shape);
            }
        }

        static Vector3 GetVertex(this Bitmap bm, int x, int y, bool mirror)
        {
            Vector3 v = new Vector3();
            System.Drawing.Color c = bm.GetPixel(x, y);
            if (mirror)
            {
                v.X = -(c.R / 255f - 0.5);
            }
            else
            {
                v.X = c.R / 255f - 0.5;
            }
            v.Y = c.G / 255f - 0.5;
            v.Z = c.B / 255f - 0.5;

            return v;
        }

        internal static Mesh SculptMeshToMesh(this Bitmap bitmap, ObjectPart.PrimitiveShape.Decoded shape)
        {
            bool mirror = shape.IsSculptMirrored;
            Mesh mesh = new Mesh();
            int vertexRowCount = bitmap.Width + 1;
            bool reverse_horizontal = shape.IsSculptInverted ? !mirror : mirror;
            PrimitiveSculptType sculptType = shape.SculptType;

            /* generate vertex map */
            for (int y = 0; y <= bitmap.Height; ++y)
            {
                for (int x = 0; x <= bitmap.Width; ++x)
                {
                    int ax, ay;
                    ay = y;
                    if (reverse_horizontal)
                    {
                        ax = bitmap.Width - 1 - x;
                    }
                    else
                    {
                        ax = x;
                    }

                    if (y == 0)
                    {
                        if (sculptType == PrimitiveSculptType.Sphere)
                        {
                            ax = bitmap.Width / 2;
                        }
                    }
                    else if (y == bitmap.Height)
                    {
                        if (sculptType == PrimitiveSculptType.Torus)
                        {
                            ay = 0;
                        }
                        else
                        {
                            ay = bitmap.Height - 1;
                        }

                        if (sculptType == PrimitiveSculptType.Sphere)
                        {
                            ax = bitmap.Width / 2;
                        }
                    }

                    if (x == bitmap.Width)
                    {
                        switch (sculptType)
                        {
                            case PrimitiveSculptType.Sphere:
                            case PrimitiveSculptType.Torus:
                            case PrimitiveSculptType.Cylinder:
                                ax = 0;
                                break;

                            default:
                                ax = bitmap.Width - 1;
                                break;
                        }
                    }

                    Vector3 v = bitmap.GetVertex(ax, ay, mirror);
                    mesh.Vertices.Add(v);
                }
            }

            /* generate triangles */
            int totalVerticeCount = mesh.Vertices.Count;

            for (int row = 0; row < totalVerticeCount - vertexRowCount; totalVerticeCount += vertexRowCount)
            {
                for(int col = 0; col < vertexRowCount - 1; ++col)
                {
                    int row2 = row;
                    Mesh.Triangle tri = new Mesh.Triangle();
                    tri.VectorIndex0 = row + col;
                    tri.VectorIndex1 = row + col + 1;
                    tri.VectorIndex2 = row2 + col + 1;
                    mesh.Triangles.Add(tri);

                    tri = new Mesh.Triangle();
                    tri.VectorIndex0 = row + col;
                    tri.VectorIndex1 = row2 + col;
                    tri.VectorIndex2 = row2 + col + 1;
                    mesh.Triangles.Add(tri);
                }
            }

            return mesh;
        }
    }
}
