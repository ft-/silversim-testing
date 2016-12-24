// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public class PhysicsConvexShape
    {
        public class ConvexHull
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();

            public ConvexHull()
            {

            }
        }

        public readonly List<ConvexHull> Hulls = new List<ConvexHull>();
        public bool HasHullList;
        public int UseCount; /* used by PhysicsShapeManager */

        public PhysicsConvexShape()
        {
        }

        public PhysicsConvexShape(byte[] data, int physOffset, int physSize)
        {
            Load(data, physOffset, physSize);
        }

        static int LEBytesToInt32(byte[] b, int offset)
        {
            byte[] data = b;
            int ofs = offset;
            if(!BitConverter.IsLittleEndian)
            {
                data = new byte[4];
                Buffer.BlockCopy(b, offset, data, 0, 4);
                Array.Reverse(data);
                ofs = 0;
            }
            return BitConverter.ToInt32(b, ofs);
        }

        static void Int32ToLEBytes(int val, byte[] b, int offset)
        {
            byte[] d = BitConverter.GetBytes(val);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            Buffer.BlockCopy(d, 0, b, offset, 4);
        }

        public byte[] SerializedData
        {
            get
            {
                int counthulls = 0;
                int countverts = 0;
                int counttris = 0;
                foreach(ConvexHull hull in Hulls)
                {
                    counthulls += 2;
                    countverts += hull.Vertices.Count;
                    counttris += hull.Triangles.Count;
                }

                byte[] createSerializedData = new byte[5 + counthulls * 8 + countverts * 12 + counttris * 4];
                int byteOffset = 5;
                createSerializedData[0] = (byte)'P';
                createSerializedData[1] = (byte)'H';
                createSerializedData[2] = (byte)'U';
                createSerializedData[3] = (byte)'L';
                createSerializedData[4] = HasHullList ? (byte)1 : (byte)0;

                foreach (ConvexHull hull in Hulls)
                {
                    Int32ToLEBytes(hull.Vertices.Count, createSerializedData, byteOffset);
                    byteOffset += 4;
                    Int32ToLEBytes(hull.Triangles.Count, createSerializedData, byteOffset);
                    byteOffset += 4;

                    foreach(Vector3 v in hull.Vertices)
                    {
                        v.ToBytes(createSerializedData, byteOffset);
                        byteOffset += 12;
                    }
                    foreach(int v in hull.Triangles)
                    {
                        Int32ToLEBytes(v, createSerializedData, byteOffset);
                        byteOffset += 4;
                    }
                }

                return createSerializedData;
            }

            set
            {
                if(null == value)
                {
                    throw new ArgumentNullException("value");
                }
                if(value[0] != (byte)'P' || value[1] != (byte)'H' || value[2] != (byte)'U' || value[3] != (byte)'L')
                {
                    throw new ArgumentException("value");
                }
                HasHullList = value[4] != 0;
                int byteOffset = 5;
                Hulls.Clear();
                while(byteOffset < value.Length)
                {
                    int vertexCount = LEBytesToInt32(value, byteOffset);
                    byteOffset += 4;
                    int triCount = LEBytesToInt32(value, byteOffset);
                    byteOffset += 4;

                    ConvexHull hull = new ConvexHull();
                    for(int idx = 0; idx < vertexCount; ++idx)
                    {
                        hull.Vertices.Add(new Vector3(value, byteOffset));
                        byteOffset += 12;
                    }

                    for(int idx = 0; idx < triCount; ++idx)
                    {
                        hull.Triangles.Add(LEBytesToInt32(value, byteOffset));
                        byteOffset += 4;
                    }
                }
            }
        }

        public void Load(byte[] data, int physOffset, int physSize)
        {
            physOffset += 2;
            physSize -= 2;
            Map physics_convex;
            using (MemoryStream ms = new MemoryStream(data, physOffset, physSize))
            {
                physics_convex = (Map)LlsdBinary.Deserialize(ms);
            }

            Vector3 min = new Vector3(-0.5, -0.5, -0.5);
            Vector3 max = new Vector3(0.5, 0.5, 0.5);

            if(physics_convex.ContainsKey("Min"))
            {
                min = physics_convex["Min"].AsVector3;
            }
            if (physics_convex.ContainsKey("Max"))
            {
                min = physics_convex["Max"].AsVector3;
            }
            Vector3 range = max - min;
            range /= 65535;
            Hulls.Clear();
            HasHullList = false;

            if(physics_convex.ContainsKey("HullList") && physics_convex.ContainsKey("Positions"))
            {
                /* we have to take multiple hulls */
                byte[] hullList = (BinaryData)physics_convex["HullList"];
                byte[] positions = (BinaryData)physics_convex["Positions"];
                int byteposition = 0;
                ConvexHull hull = new ConvexHull();
                foreach(byte b in hullList)
                {
                    int hullElements = b == 0 ? 256 : (int)b;
                    for(int idx = 0; idx < hullElements; ++idx)
                    {
                        uint x = positions[byteposition++];
                        x |= (uint)(positions[byteposition++] << 8);
                        uint y = positions[byteposition++];
                        y |= (uint)(positions[byteposition++] << 8);
                        uint z = positions[byteposition++];
                        z |= (uint)(positions[byteposition++] << 8);

                        Vector3 v = new Vector3(
                            x, y, z);
                        hull.Vertices.Add(v.ElementMultiply(range) + min);
                        hull.Triangles.Add(idx);
                    }
                }
                Hulls.Add(hull);
                HasHullList = true;
            }
            else if(physics_convex.ContainsKey("BoundingVerts"))
            {
                byte[] positions = (BinaryData)physics_convex["BoundingVerts"];
                int hullElements = positions.Length / 6;
                int byteposition = 0;
                ConvexHull hull = new ConvexHull();
                for (int idx = 0; idx < hullElements; ++idx)
                {
                    uint x = positions[byteposition++];
                    x |= (uint)(positions[byteposition++] << 8);
                    uint y = positions[byteposition++];
                    y |= (uint)(positions[byteposition++] << 8);
                    uint z = positions[byteposition++];
                    z |= (uint)(positions[byteposition++] << 8);

                    Vector3 v = new Vector3(
                        x, y, z);
                    hull.Vertices.Add(v.ElementMultiply(range) + min);
                    hull.Triangles.Add(idx);
                }
                Hulls.Add(hull);
            }
            else
            {
                throw new NoSuchMeshDataException();
            }
        }

        static string VertexToString(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", v.X, v.Y, v.Z);
        }

        public void DumpToBlenderRaw(string filename)
        {
            /* write a blender .raw */
            using (StreamWriter w = new StreamWriter(filename))
            {
                foreach(ConvexHull hull in Hulls)
                {
                    int triidx;
                    for(triidx = 0; triidx < hull.Triangles.Count; triidx += 3)
                    {
                        w.WriteLine("{0} {1} {2}",
                            VertexToString(hull.Vertices[hull.Triangles[triidx + 0]]),
                            VertexToString(hull.Vertices[hull.Triangles[triidx + 1]]),
                            VertexToString(hull.Vertices[hull.Triangles[triidx + 2]]));
                    }
                }
            }
        }
    }
}
