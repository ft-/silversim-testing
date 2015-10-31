// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Primitive;
using System;
using System.IO;
using System.IO.Compression;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static class LLMesh
    {
        internal static Mesh LLMeshToMesh(this AssetData data, ObjectPart.PrimitiveShape.Decoded shape, bool usePhysicsMesh)
        {
            if (data.Type != AssetType.Mesh)
            {
                throw new InvalidLLMeshAssetException();
            }
            using (Stream s = data.InputStream)
            {
                return s.LLMeshToMesh(shape, usePhysicsMesh);
            }
        }

        internal static Mesh LLMeshToMesh(this Stream st, ObjectPart.PrimitiveShape.Decoded shape, bool usePhysicsMesh)
        {
            /* this is a format of multiple parts */
            Map rootmap = (Map)LlsdBinary.Deserialize(st);
            Map physicsParameter;

            if (rootmap.ContainsKey("physics_shape"))
            {
                physicsParameter = (Map)rootmap["physics_shape"];
            }
            else if (rootmap.ContainsKey("physics_mesh"))
            {
                physicsParameter = (Map)rootmap["physics_mesh"];
            }
            else if (rootmap.ContainsKey("medium_lod"))
            {
                physicsParameter = (Map)rootmap["medium_lod"];
            }
            else if (rootmap.ContainsKey("high_lod"))
            {
                physicsParameter = (Map)rootmap["high_lod"];
            }
            else
            {
                throw new InvalidLLMeshAssetException();
            }

            int physOffset = physicsParameter["offset"].AsInt;
            int physSize = physicsParameter["size"].AsInt;

            byte[] b = new byte[10240];
            while(physOffset > 0)
            {
                int res;
                if(physOffset > b.Length)
                {
                    res = st.Read(b, 0, 1024);
                }
                else
                {
                    res = st.Read(b, 0, physOffset);
                }
                if (res > 0)
                {
                    physOffset += res;
                }
                else
                {
                    throw new InvalidLLMeshAssetException();
                }
            }

            AnArray meshArray;
            using(GZipStream gz = new GZipStream(st, CompressionMode.Decompress))
            {
                try
                {
                    meshArray = (AnArray)LlsdBinary.Deserialize(gz);
                }
                catch
                {
                    throw new InvalidLLMeshAssetException();
                }
            }


            Mesh mesh = new Mesh();

            foreach(IValue submesh_v in meshArray)
            {
                Map submesh = submesh_v as Map;
                if(null == submesh)
                {
                    continue;
                }
                if(submesh.ContainsKey("NoGeometry") && submesh["NoGeometry"].AsBoolean)
                {
                    continue;
                }

                Vector3 maxPos = ((Map)(submesh["PositionDomain"]))["Max"].AsVector3;
                Vector3 minPos = ((Map)(submesh["PositionDomain"]))["Min"].AsVector3;
                int vertexIndexOffset = mesh.Vertices.Count;

                byte[] posBytes = (BinaryData)submesh["Position"];
                for (int i = 0; i < posBytes.Length; i += 6)
                {
                    ushort uX = (ushort)((posBytes[i + 1] << 8) | posBytes[i + 0]);
                    ushort uY = (ushort)((posBytes[i + 3] << 8) | posBytes[i + 2]);
                    ushort uZ = (ushort)((posBytes[i + 5] << 8) | posBytes[i + 4]);

                    Vector3 v = new Vector3();
                    v.X = minPos.X.Lerp(maxPos.X, uX / 65535f);
                    v.Y = minPos.Y.Lerp(maxPos.Y, uY / 65535f);
                    v.Z = minPos.Z.Lerp(maxPos.Z, uZ / 65535f);
                    mesh.Vertices.Add(v);
                }

                byte[] triangleBytes = (BinaryData)submesh["TriangleList"];
                for (int i = 0; i < triangleBytes.Length; i += 6)
                {
                    ushort v1 = (ushort)((posBytes[i + 1] << 8) | posBytes[i + 0]);
                    ushort v2 = (ushort)((posBytes[i + 3] << 8) | posBytes[i + 2]);
                    ushort v3 = (ushort)((posBytes[i + 5] << 8) | posBytes[i + 4]);
                    Mesh.Triangle tri = new Mesh.Triangle();
                    tri.VectorIndex0 = v1 + vertexIndexOffset;
                    tri.VectorIndex1 = v2 + vertexIndexOffset;
                    tri.VectorIndex2 = v3 + vertexIndexOffset;
                    mesh.Triangles.Add(tri);
                }
            }
            return mesh;
        }
    }
}
