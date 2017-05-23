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

using CSJ2K;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System;
using System.Drawing;
using System.IO;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static class SculptMesh
    {
        internal static MeshLOD SculptMeshToMesh(this AssetData data, ObjectPart.PrimitiveShape.Decoded shape)
        {
            if(data.Type != AssetType.Texture)
            {
                throw new InvalidSculptMeshAssetException();
            }
            using (var s = data.InputStream)
            {
                return s.SculptMeshToMesh(shape);
            }
        }

        internal static MeshLOD SculptMeshToMesh(this Stream st, ObjectPart.PrimitiveShape.Decoded shape)
        {
            using (var im = J2kImage.FromStream(st))
            {
                using (var bitmap = new Bitmap(im))
                {
                    return bitmap.SculptMeshToMesh(shape);
                }
            }
        }

        private static Vector3 GetVertex(this Bitmap bm, int x, int y, bool mirror)
        {
            var v = new Vector3();
            var c = bm.GetPixel(x, y);
            v.X = mirror ?
                -(c.R / 255f - 0.5) :
                c.R / 255f - 0.5;
            v.Y = c.G / 255f - 0.5;
            v.Z = c.B / 255f - 0.5;

            return v;
        }

        internal static MeshLOD SculptMeshToMesh(this Bitmap bitmap, ObjectPart.PrimitiveShape.Decoded shape, bool generate_uv = false)
        {
            bool mirror = shape.IsSculptMirrored;
            var mesh = new MeshLOD();
            bool reverse_horizontal = shape.IsSculptInverted ? !mirror : mirror;
            PrimitiveSculptType sculptType = shape.SculptType;

            int sculptSizeS;
            int sculptSizeT;
            int sculptVerts = bitmap.Width * bitmap.Height / 4;
            if(sculptVerts > 32 * 32)
            {
                sculptVerts = 32 * 32;
            }
            double ratio = (double)bitmap.Width / bitmap.Height;

            sculptSizeS = (int)Math.Sqrt(sculptVerts / ratio);

            sculptSizeS = Math.Max(sculptSizeS, 4);
            sculptSizeT = sculptVerts / sculptSizeS;

            sculptSizeT = Math.Max(sculptSizeT, 4);
            sculptSizeS = sculptVerts / sculptSizeT;

            /* generate vertex map */
            for (int s = 0; s < sculptSizeS; ++s)
            {
                for (int t = 0; t < sculptSizeT; ++t)
                {
                    int reversed_t = t;
                    if(reverse_horizontal)
                    {
                        reversed_t = sculptSizeT - t - 1;
                    }
                    int x = (int)((double)reversed_t / (sculptSizeT - 1) * bitmap.Width);
                    int y = (int)((double)s / (sculptSizeS - 1) * bitmap.Height);

                    if (y == 0)
                    {
                        if (sculptType == PrimitiveSculptType.Sphere)
                        {
                            x = bitmap.Width / 2;
                        }
                    }
                    else if (y == bitmap.Height)
                    {
                        y = (sculptType == PrimitiveSculptType.Torus) ?
                            0 :
                            bitmap.Height - 1;

                        if (sculptType == PrimitiveSculptType.Sphere)
                        {
                            x = bitmap.Width / 2;
                        }
                    }

                    if (x == bitmap.Width)
                    {
                        switch (sculptType)
                        {
                            case PrimitiveSculptType.Sphere:
                            case PrimitiveSculptType.Torus:
                            case PrimitiveSculptType.Cylinder:
                                x = 0;
                                break;

                            default:
                                x = bitmap.Width - 1;
                                break;
                        }
                    }

                    Vector3 v = bitmap.GetVertex(x, y, mirror);
                    mesh.Vertices.Add(v);
                    if(generate_uv)
                    {
                        var uv = new UVCoord()
                        {
                            U = (float)reversed_t / (sculptSizeS - t),
                            V = (float)s / (sculptSizeS - 1)
                        };
                        mesh.UVCoords.Add(uv);
                    }
                }
            }

            /* generate triangles */
            for (int row = 0; row < sculptSizeS - 1; ++row)
            {
                int rowIndex = row * sculptSizeT;
                int row2Index = rowIndex + sculptSizeT;
                for(int col = 0; col < sculptSizeT - 1; ++col)
                {
                    var tri = new Triangle()
                    {
                        Vertex1 = rowIndex + col,
                        Vertex2 = row2Index + col + 1,
                        Vertex3 = rowIndex + col + 1
                    };
                    mesh.Triangles.Add(tri);

                    tri = new Triangle()
                    {
                        Vertex1 = rowIndex + col,
                        Vertex2 = row2Index + col,
                        Vertex3 = row2Index + col + 1
                    };
                    mesh.Triangles.Add(tri);
                }
            }

            return mesh;
        }
    }
}
