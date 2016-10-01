// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public struct UVCoord
    {
        public float U;
        public float V;

        public UVCoord(float u, float v)
        {
            U = u;
            V = v;
        }
    }

    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct Triangle
    {
        public int FaceIdx;

        public int Vertex1;
        public int Vertex2;
        public int Vertex3;

        public Triangle(int vertex1, int vertex2, int vertex3)
        {
            FaceIdx = 0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
        }
    }

    public class MeshLOD
    {
        public MeshLOD()
        {

        }

        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<UVCoord> UVCoords = new List<UVCoord>();
        public List<Triangle> Triangles = new List<Triangle>();

        public MeshLOD(byte[] data, int physOffset, int physSize)
        {
            Load(data, physOffset, physSize);
        }

        static Vector3 U16LEBytesToVertex(byte[] data, int offs, Vector3 posMin, Vector3 posMax)
        {
            byte[] buf = data;
            if(!BitConverter.IsLittleEndian)
            {
                buf = new byte[6];
                Buffer.BlockCopy(data, offs, buf, 0, 6);
                Array.Reverse(buf, 0, 2);
                Array.Reverse(buf, 2, 2);
                Array.Reverse(buf, 4, 2);
                offs = 0;
            }

            ushort vx = BitConverter.ToUInt16(buf, offs);
            ushort vy = BitConverter.ToUInt16(buf, offs + 2);
            ushort vz = BitConverter.ToUInt16(buf, offs + 4);
            float x = (float)((vx * (posMax.X - posMin.X)) / 65535f + posMin.X);
            float y = (float)((vy * (posMax.Y - posMin.Y)) / 65535f + posMin.Y);
            float z = (float)((vz * (posMax.Z - posMin.Z)) / 65535f + posMin.Z);
            return new Vector3(x, y, z);
        }

        static UVCoord U16LEBytesToUV(byte[] data, int offs, UVCoord posMin, UVCoord posMax)
        {
            byte[] buf = data;
            if (!BitConverter.IsLittleEndian)
            {
                buf = new byte[6];
                Buffer.BlockCopy(data, offs, buf, 0, 6);
                Array.Reverse(buf, 0, 2);
                Array.Reverse(buf, 2, 2);
                Array.Reverse(buf, 4, 2);
                offs = 0;
            }

            ushort vx = BitConverter.ToUInt16(buf, offs);
            ushort vy = BitConverter.ToUInt16(buf, offs + 2);
            float u = (vx * (posMax.U - posMin.U)) / 65535f + posMin.U;
            float v = (vy * (posMax.V - posMin.V)) / 65535f + posMin.V;
            return new UVCoord(u, v);
        }

        static float BytesLEToFloat(byte[] data, int offs)
        {
            byte[] buf = data;
            if(!BitConverter.IsLittleEndian)
            {
                buf = new byte[4];
                Buffer.BlockCopy(data, offs, buf, 0, 4);
                Array.Reverse(buf);
                offs = 0;
            }

            return BitConverter.ToSingle(data, offs);
        }

        static ushort BytesLEToU16(byte[] data, int offs)
        {
            byte[] buf = data;
            if (!BitConverter.IsLittleEndian)
            {
                buf = new byte[2];
                Buffer.BlockCopy(data, offs, buf, 0, 2);
                Array.Reverse(buf);
                offs = 0;
            }

            return BitConverter.ToUInt16(buf, offs);
        }

        /* do not use Optimize for visual meshes */
        public void Optimize()
        {
            /* collapse all identical vertices */
            List<Vector3> NewVertices = new List<Vector3>();
            Dictionary<int, int> VertexMap = new Dictionary<int, int>();
            List<Triangle> NewTriangles = new List<Triangle>();

            /* identify all duplicate meshes */
            for (int i = 0; i < Vertices.Count; ++i)
            {
                Vector3 v = Vertices[i];
                int newIdx = NewVertices.IndexOf(v);
                if (newIdx < 0)
                {
                    newIdx = NewVertices.Count;
                    NewVertices.Add(v);
                }
                VertexMap.Add(i, newIdx);
            }

            /* remap all vertices */
            for (int i = 0; i < Triangles.Count; ++i)
            {
                Triangle tri = Triangles[i];
                tri.Vertex1 = VertexMap[tri.Vertex1];
                tri.Vertex2 = VertexMap[tri.Vertex2];
                tri.Vertex3 = VertexMap[tri.Vertex3];

                if (tri.Vertex1 != tri.Vertex2 && tri.Vertex1 != tri.Vertex3 &&
                    tri.Vertex2 != tri.Vertex3)
                {
                    /* 0 is != 1 and 0 != 2 and 1 != 2
                     * 
                     * so, 1 cannot be either 0 or 2.
                     * so, 0 cannot be either 1 or 2.
                     * so, 2 cannot be either 0 or 1.
                     * 
                     * This makes a nice non-degenerate triangle.
                     */
                    NewTriangles.Add(tri);
                }
            }

            /* use new vertex list */
            Vertices = NewVertices;

            /* use new triangle list */
            Triangles = NewTriangles;

            /* clear normals and UVCoords we use their place anyways here */
            Normals.Clear();
            UVCoords.Clear();
        }

        protected void Load(byte[] data, int physOffset, int physSize)
        {
            physOffset += 2;
            physSize -= 2;
            AnArray submeshes;
            using (MemoryStream ms = new MemoryStream(data, physOffset, physSize))
            {
                submeshes = (AnArray)LlsdBinary.Deserialize(ms);
            }
            int faceNo = 0;
            foreach(IValue iv in submeshes)
            {
                Map submesh = (Map)iv;
                Vector3 posMax = new Vector3(0.5, 0.5, 0.5);
                Vector3 posMin = new Vector3(-0.5, -0.5, -0.5);
                UVCoord uvMax = new UVCoord();
                UVCoord uvMin = new UVCoord();

                if(submesh.ContainsKey("PositionDomain"))
                {
                    Map posDom = (Map)submesh["PositionDomain"];
                    if (posDom.ContainsKey("Max"))
                    {
                        posMax = posDom["Max"].AsVector3;
                    }
                    if (posDom.ContainsKey("Min"))
                    {
                        posMin = posDom["Min"].AsVector3;
                    }
                }
                byte[] posBytes = (BinaryData)submesh["Position"];
                byte[] normalBytes = null;
                byte[] texcoordBytes = null;
                if (submesh.ContainsKey("Normal"))
                {
                    normalBytes = (BinaryData)submesh["Normal"];
                }
                if(submesh.ContainsKey("TexCoord0"))
                {
                    texcoordBytes = (BinaryData)submesh["TexCoord0"];
                    Map texDom = (Map)submesh["TexCoord0Domain"];
                    if(texDom.ContainsKey("Max"))
                    {
                        byte[] domData = (BinaryData)texDom["Max"];
                        uvMax.U = BytesLEToFloat(domData, 0);
                        uvMax.V = BytesLEToFloat(domData, 4);
                    }
                    if(texDom.ContainsKey("Min"))
                    {
                        byte[] domData = (BinaryData)texDom["Min"];
                        uvMin.U = BytesLEToFloat(domData, 0);
                        uvMin.V = BytesLEToFloat(domData, 4);
                    }
                }
                ushort faceIndexOffset = (ushort)Vertices.Count;
                int uvBytePos = 0;
                for(int i = 0; i< posBytes.Length; i += 6)
                {
                    Vertices.Add(U16LEBytesToVertex(posBytes, i, posMin, posMax));
                    if(normalBytes != null)
                    {
                        Normals.Add(U16LEBytesToVertex(normalBytes, i, posMin, posMax));
                    }
                    if (texcoordBytes != null)
                    {
                        UVCoords.Add(U16LEBytesToUV(texcoordBytes, uvBytePos, uvMin, uvMax));
                        uvBytePos += 4;
                    }
                }

                byte[] triangleBytes = (BinaryData)submesh["TriangleList"];
                for(int i = 0; i < triangleBytes.Length; i += 6)
                {
                    ushort v1 = (ushort)(BytesLEToU16(triangleBytes, i) + faceIndexOffset);
                    ushort v2 = (ushort)(BytesLEToU16(triangleBytes, i + 2) + faceIndexOffset);
                    ushort v3 = (ushort)(BytesLEToU16(triangleBytes, i + 4) + faceIndexOffset);
                    Triangle t = new Triangle(v1, v2, v3);
                    t.FaceIdx = faceNo;
                    Triangles.Add(t);
                }
            }
        }
    }
}
