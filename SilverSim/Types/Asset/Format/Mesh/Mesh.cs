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

using Ionic.Zlib;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        private static Vector3 U16LEBytesToVertex(byte[] data, int offset, Vector3 posMin, Vector3 posMax)
        {
            int offs = offset;
            var buf = data;
            if(!BitConverter.IsLittleEndian)
            {
                buf = new byte[6];
                Buffer.BlockCopy(data, offs, buf, 0, 6);
                Array.Reverse(buf, 0, 2);
                Array.Reverse(buf, 2, 2);
                Array.Reverse(buf, 4, 2);
                offs = 0;
            }

            var vx = BitConverter.ToUInt16(buf, offs);
            var vy = BitConverter.ToUInt16(buf, offs + 2);
            var vz = BitConverter.ToUInt16(buf, offs + 4);
            var x = (vx * (posMax.X - posMin.X) / 65535f) + posMin.X;
            var y = (vy * (posMax.Y - posMin.Y) / 65535f) + posMin.Y;
            var z = (vz * (posMax.Z - posMin.Z) / 65535f) + posMin.Z;
            return new Vector3(x, y, z);
        }

        private static UVCoord U16LEBytesToUV(byte[] data, int offset, UVCoord posMin, UVCoord posMax)
        {
            int offs = offset;
            var buf = data;
            if (!BitConverter.IsLittleEndian)
            {
                buf = new byte[6];
                Buffer.BlockCopy(data, offs, buf, 0, 6);
                Array.Reverse(buf, 0, 2);
                Array.Reverse(buf, 2, 2);
                Array.Reverse(buf, 4, 2);
                offs = 0;
            }

            var vx = BitConverter.ToUInt16(buf, offs);
            var vy = BitConverter.ToUInt16(buf, offs + 2);
            var u = (vx * (posMax.U - posMin.U) / 65535f) + posMin.U;
            var v = (vy * (posMax.V - posMin.V) / 65535f) + posMin.V;
            return new UVCoord(u, v);
        }

        private static float BytesLEToFloat(byte[] data, int offset)
        {
            var buf = data;
            int offs = offset;
            if(!BitConverter.IsLittleEndian)
            {
                buf = new byte[4];
                Buffer.BlockCopy(data, offs, buf, 0, 4);
                Array.Reverse(buf);
                offs = 0;
            }

            return BitConverter.ToSingle(data, offs);
        }

        private static ushort BytesLEToU16(byte[] data, int offset)
        {
            var buf = data;
            int offs = offset;
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
            var NewVertices = new List<Vector3>();
            var VertexMap = new Dictionary<int, int>();
            var NewTriangles = new List<Triangle>();

            /* identify all duplicate vertices */
            for (int i = 0; i < Vertices.Count; ++i)
            {
                var v = Vertices[i];
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
                var tri = Triangles[i];
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
            AnArray submeshes;
            using (var ms = new MemoryStream(data, physOffset, physSize))
            {
                using (var gz = new ZlibStream(ms, CompressionMode.Decompress))
                {
                    submeshes = (AnArray)LlsdBinary.Deserialize(gz);
                }
            }
            int faceNo = 0;
            foreach(var iv in submeshes)
            {
                var submesh = (Map)iv;
                var posMax = new Vector3(0.5, 0.5, 0.5);
                var posMin = new Vector3(-0.5, -0.5, -0.5);
                var uvMax = new UVCoord();
                var uvMin = new UVCoord();

                if(submesh.ContainsKey("PositionDomain"))
                {
                    var posDom = (Map)submesh["PositionDomain"];
                    if (posDom.ContainsKey("Max"))
                    {
                        var ivdom = (AnArray)posDom["Max"];
                        posMax = new Vector3(ivdom[0].AsReal, ivdom[1].AsReal, ivdom[2].AsReal);
                    }
                    if (posDom.ContainsKey("Min"))
                    {
                        var ivdom = (AnArray)posDom["Min"];
                        posMin = new Vector3(ivdom[0].AsReal, ivdom[1].AsReal, ivdom[2].AsReal);
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
                    var texDom = (Map)submesh["TexCoord0Domain"];
                    if(texDom.ContainsKey("Max"))
                    {
                        var domData = (AnArray)texDom["Max"];
                        uvMax.U = (float)domData[0].AsReal;
                        uvMax.V = (float)domData[1].AsReal;
                    }
                    if(texDom.ContainsKey("Min"))
                    {
                        var domData = (AnArray)texDom["Min"];
                        uvMin.U = (float)domData[0].AsReal;
                        uvMin.V = (float)domData[1].AsReal;
                    }
                }
                var faceIndexOffset = (ushort)Vertices.Count;
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
                    var v1 = (ushort)(BytesLEToU16(triangleBytes, i) + faceIndexOffset);
                    var v2 = (ushort)(BytesLEToU16(triangleBytes, i + 2) + faceIndexOffset);
                    var v3 = (ushort)(BytesLEToU16(triangleBytes, i + 4) + faceIndexOffset);
                    var t = new Triangle(v1, v2, v3)
                    {
                        FaceIdx = faceNo
                    };
                    Triangles.Add(t);
                }
                ++faceNo;
            }
        }

        private static string VertexToString(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", v.X, v.Y, v.Z);

        public void DumpToBlenderRaw(string filename)
        {
            /* write a blender .raw */
            using (var w = new StreamWriter(filename))
            {
                foreach (var tri in Triangles)
                {
                    w.WriteLine("{0} {1} {2}",
                        VertexToString(Vertices[tri.Vertex1]),
                        VertexToString(Vertices[tri.Vertex2]),
                        VertexToString(Vertices[tri.Vertex3]));
                }
            }
        }
    }
}
