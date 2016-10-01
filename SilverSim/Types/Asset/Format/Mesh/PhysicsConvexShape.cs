// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public class PhysicsConvexShape
    {
        public readonly List<List<Vector3>> Hulls = new List<List<Vector3>>();
        public bool HasHullList = false;

        public PhysicsConvexShape()
        {
        }

        public PhysicsConvexShape(byte[] data, int physOffset, int physSize)
        {
            Load(data, physOffset, physSize);
        }

        public byte[] SerializedData
        {
            get
            {
                int counthulls = 0;
                int countverts = 0;
                foreach(List<Vector3> hull in Hulls)
                {
                    ++counthulls;
                    countverts += hull.Count;
                }

                byte[] serializedData = new byte[5 + counthulls * sizeof(int) + countverts * 12];
                int byteOffset = 5;
                serializedData[0] = (byte)'P';
                serializedData[1] = (byte)'H';
                serializedData[2] = (byte)'U';
                serializedData[3] = (byte)'L';
                SerializedData[4] = HasHullList ? (byte)1 : (byte)0;
                foreach (List<Vector3> hull in Hulls)
                {
                    byte[] d = BitConverter.GetBytes(hull.Count);
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(d);
                    }
                    Buffer.BlockCopy(d, 0, serializedData, byteOffset, 4);
                    byteOffset += 4;

                    foreach(Vector3 v in hull)
                    {
                        v.ToBytes(serializedData, byteOffset);
                        byteOffset += 12;
                    }
                }

                return serializedData;
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
                    int hullCount;
                    if(!BitConverter.IsLittleEndian)
                    {
                        byte[] d = new byte[4];
                        Buffer.BlockCopy(value, byteOffset, d, 0, 4);
                        Array.Reverse(d);
                        hullCount = BitConverter.ToInt32(d, 0);
                    }
                    else
                    {
                        hullCount = BitConverter.ToInt32(value, byteOffset);
                    }
                    byteOffset += 4;

                    List<Vector3> hull = new List<Vector3>();
                    for(int idx = 0; idx < hullCount; ++idx)
                    {
                        Vector3 v = new Vector3();
                        v.FromBytes(value, byteOffset);
                        byteOffset += 12;
                        hull.Add(v);
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
                List<Vector3> hull = new List<Vector3>();
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
                        hull.Add(v.ElementMultiply(range) + min);
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
                List<Vector3> hull = new List<Vector3>();
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
                    hull.Add(v.ElementMultiply(range) + min);
                }
                Hulls.Add(hull);
            }
            else
            {
                throw new NoSuchMeshDataException();
            }
        }
    }
}
