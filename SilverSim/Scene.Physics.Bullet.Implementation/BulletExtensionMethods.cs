// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using BulletSharp;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Mesh;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Physics.Bullet.Implementation
{
    public static class BulletExtensionMethods
    {
        public static IndexedMesh ToBulletMesh(this Mesh mesh, Vector3 position, Vector3 scale, Quaternion rot)
        {
            IndexedMesh bulletmesh = new IndexedMesh();
            bulletmesh.Allocate(mesh.Vertices.Count, 1, mesh.Triangles.Count * 3, 1);
            int vidx = 0;
            foreach (Vector3 vi in mesh.Vertices)
            {
                Vector3 v = vi;
                v = v* rot;
                v.X = v.X * scale.X;
                v.Y = v.Y * scale.Y;
                v.Z = v.Z * scale.Z;
                v += position;
                bulletmesh.Vertices[vidx++] = v;
            }

            int tridx = 0;
            foreach (Mesh.Triangle tri in mesh.Triangles)
            {
                bulletmesh.TriangleIndices[tridx++] = tri.VectorIndex0;
                bulletmesh.TriangleIndices[tridx++] = tri.VectorIndex1;
                bulletmesh.TriangleIndices[tridx++] = tri.VectorIndex2;
            }

            return bulletmesh;
        }

        public static TriangleIndexVertexMaterialArray ToBulletMesh(this ObjectGroup grp, AssetServiceInterface assetService)
        {
            TriangleIndexVertexMaterialArray vma = new TriangleIndexVertexMaterialArray();
            foreach(ObjectPart part in grp.Values)
            {
                /* skip all non contributing parts */
                if (part.PhysicsShapeType != SilverSim.Types.Primitive.PrimitivePhysicsShapeType.None)
                {
                    Mesh m = part.ToMesh(assetService);
                    Vector3 scale = part.Size;
                    Vector3 position = part.LocalPosition;
                    Quaternion rotation = part.LocalRotation;
                    if (part == grp.RootPart)
                    {
                        position = Vector3.Zero;
                        rotation = Quaternion.Identity;
                    }
                    IndexedMesh imesh = m.ToBulletMesh(position, scale, rotation);
                    vma.AddIndexedMesh(imesh);
                }
            }

            return vma;
        }
    }
}
