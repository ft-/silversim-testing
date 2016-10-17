// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using CSJ2K;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System.Diagnostics.CodeAnalysis;
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
            using (Stream s = data.InputStream)
            {
                return s.SculptMeshToMesh(shape);
            }
        }

        internal static MeshLOD SculptMeshToMesh(this Stream st, ObjectPart.PrimitiveShape.Decoded shape)
        {
            using (Image im = J2kImage.FromStream(st))
            {
                using (Bitmap bitmap = new Bitmap(im))
                {
                    return bitmap.SculptMeshToMesh(shape);
                }
            }
        }

        static Vector3 GetVertex(this Bitmap bm, int x, int y, bool mirror)
        {
            Vector3 v = new Vector3();
            System.Drawing.Color c = bm.GetPixel(x, y);
            v.X = mirror ?
                -(c.R / 255f - 0.5) :
                c.R / 255f - 0.5;
            v.Y = c.G / 255f - 0.5;
            v.Z = c.B / 255f - 0.5;

            return v;
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        internal static MeshLOD SculptMeshToMesh(this Bitmap bitmap, ObjectPart.PrimitiveShape.Decoded shape)
        {
            bool mirror = shape.IsSculptMirrored;
            MeshLOD mesh = new MeshLOD();
            int vertexRowCount = bitmap.Width + 1;
            bool reverse_horizontal = shape.IsSculptInverted ? !mirror : mirror;
            PrimitiveSculptType sculptType = shape.SculptType;

            /* generate vertex map */
            for (int y = 0; y <= bitmap.Height; ++y)
            {
                for (int x = 0; x <= bitmap.Width; ++x)
                {
                    int ax;
                    int ay;

                    ay = y;
                    ax = reverse_horizontal ?
                        bitmap.Width - 1 - x :
                        x;

                    if (y == 0)
                    {
                        if (sculptType == PrimitiveSculptType.Sphere)
                        {
                            ax = bitmap.Width / 2;
                        }
                    }
                    else if (y == bitmap.Height)
                    {
                        ay = (sculptType == PrimitiveSculptType.Torus) ?
                            0 :
                            bitmap.Height - 1;

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

            for (int row = 0; row < totalVerticeCount - vertexRowCount; row += vertexRowCount)
            {
                for(int col = 0; col < vertexRowCount - 1; ++col)
                {
                    int row2 = row + 1;
                    Triangle tri = new Triangle();
                    tri.Vertex1 = row + col;
                    tri.Vertex2 = row + col + 1;
                    tri.Vertex3 = row2 + col + 1;
                    mesh.Triangles.Add(tri);

                    tri = new Triangle();
                    tri.Vertex1 = row + col;
                    tri.Vertex2 = row2 + col;
                    tri.Vertex3 = row2 + col + 1;
                    mesh.Triangles.Add(tri);
                }
            }

            return mesh;
        }
    }
}
