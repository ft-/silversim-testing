// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public sealed class Mesh
    {
        public struct Triangle
        {
            public int PrimFaceIndex;

            public int VectorIndex0;
            public int VectorIndex1;
            public int VectorIndex2;

            public int NormalIndex0;
            public int NormalIndex1;
            public int NormalIndex2;

            public int UVIndex0;
            public int UVIndex1;
            public int UVIndex2;
        }

        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Triangle> Triangles = new List<Triangle>();

        public Mesh()
        {

        }

        public void Optimize()
        {
            /* collapse all identical vertices */
            List<Vector3> NewVertices = new List<Vector3>();
            Dictionary<int, int> VertexMap = new Dictionary<int,int>();
            List<Triangle> NewTriangles = new List<Triangle>();

            /* identify all duplicate meshes */
            for(int i = 0; i < Vertices.Count; ++i)
            {
                Vector3 v = Vertices[i];
                int newIdx = NewVertices.IndexOf(v);
                if(newIdx < 0)
                {
                    newIdx = NewVertices.Count;
                    NewVertices.Add(v);
                }
                VertexMap.Add(i, newIdx);
            }

            /* remap all vertices */
            for(int i = 0; i < Triangles.Count; ++i)
            {
                Triangle tri = Triangles[i];
                tri.VectorIndex0 = VertexMap[tri.VectorIndex0];
                tri.VectorIndex1 = VertexMap[tri.VectorIndex1];
                tri.VectorIndex2 = VertexMap[tri.VectorIndex2];

                if(tri.VectorIndex0 != tri.VectorIndex1 && tri.VectorIndex0 != tri.VectorIndex2 &&
                    tri.VectorIndex1 != tri.VectorIndex2)
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
        }
    }
}
