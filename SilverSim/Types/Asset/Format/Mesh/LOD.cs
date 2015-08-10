// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.StructuredData.LLSD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public struct Vertex
    {
        public float X;
        public float Y;
        public float Z;
        public Vertex(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vertex(Vertex v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }
    }

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

        public int Normal1;
        public int Normal2;
        public int Normal3;

        public int UV1;
        public int UV2;
        public int UV3;

        public Triangle(int vertex1, int vertex2, int vertex3)
        {
            FaceIdx = 0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
            Normal1 = 0;
            Normal2 = 0;
            Normal3 = 0;
            UV1 = 0;
            UV2 = 0;
            UV3 = 0;
        }

        public Triangle(int vertex1, int vertex2, int vertex3, int normal1, int normal2, int normal3)
        {
            FaceIdx = 0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
            Normal1 = normal1;
            Normal2 = normal2;
            Normal3 = normal3;
            UV1 = 0;
            UV2 = 0;
            UV3 = 0;
        }
    }
    public class LOD
    {
        public LOD()
        {

        }

        public List<Vertex> Vertices = new List<Vertex>();
        public List<UVCoord> UVCoords = new List<UVCoord>();
        public List<Triangle> Triangles = new List<Triangle>();

        public LOD(byte[] data, int physOffset, int physSize)
        {
            Load(data, physOffset, physSize);
        }

        static Vertex U16LEBytesToVertex(byte[] data, int offs, Vector3 posMin, Vector3 posMax)
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
            return new Vertex(x, y, z);
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

        protected void Load(byte[] data, int physOffset, int physSize)
        {
            physOffset += 2;
            physSize -= 2;
            AnArray submeshes;
            using (MemoryStream ms = new MemoryStream(data, physOffset, physSize))
            {
                submeshes = (AnArray)LLSD_Binary.Deserialize(ms);
            }
            foreach(IValue iv in submeshes)
            {
                Map submesh = (Map)iv;
                Vector3 posMax = ((Map)submesh["PositionDomain"])["Max"].AsVector3;
                Vector3 posMin = ((Map)submesh["PositionDomain"])["Min"].AsVector3;
                byte[] posBytes = (BinaryData)submesh["Position"];
                ushort faceIndexOffset = (ushort)Vertices.Count;
                for(int i = 0; i< posBytes.Length; i += 6)
                {
                    Vertices.Add(U16LEBytesToVertex(posBytes, i, posMin, posMax));
                }

                byte[] triangleBytes = (BinaryData)submesh["TriangleList"];
                for(int i = 0; i < triangleBytes.Length; i += 6)
                {
                    ushort v1 = (ushort)(BytesLEToU16(triangleBytes, i) + faceIndexOffset);
                    ushort v2 = (ushort)(BytesLEToU16(triangleBytes, i + 2) + faceIndexOffset);
                    ushort v3 = (ushort)(BytesLEToU16(triangleBytes, i + 4) + faceIndexOffset);
                    Triangle t = new Triangle(v1, v2, v3);
                    Triangles.Add(t);
                }
            }
        }
    }
}
