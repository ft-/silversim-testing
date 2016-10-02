// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SilverSim.Scene.Physics.ShapeManager
{
    public class VHACD : IDisposable
    {
        public struct ConvexHull
        {
            public IntPtr Points;
            public IntPtr Triangles;
            public int NumPoints;
            public int NumTriangles;
        }

        public struct Parameters
        {
            public double Concavity;
            public double Alpha;
            public double Beta;
            public double Gamma;
            public double MinVolumePerCH;
            public IntPtr Callback;
            public IntPtr Logger;
            public uint Resolution;
            public uint MaxNumVerticesPerCH;
            public int Depth;
            public int PlaneDownsampling;
            public int ConvexhullDownsampling;
            public int Pca;
            public int Mode;
            public bool ConvexhullApproximation;
            public bool OclAcceleration;

            public void Init()
            {
                Resolution = 100000;
                Depth = 20;
                Concavity = 0.001;
                PlaneDownsampling = 4;
                ConvexhullDownsampling = 4;
                Alpha = 0.05;
                Beta = 0.05;
                Gamma = 0.0005;
                Pca = 0;
                Mode = 0; // 0: voxel-based (recommended), 1: tetrahedron-based
                MaxNumVerticesPerCH = 64;
                MinVolumePerCH = 0.0001;
                Callback = IntPtr.Zero;
                Logger = IntPtr.Zero;
                ConvexhullApproximation = true;
                OclAcceleration = true;
            }
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string dllToLoad);

        static object m_InitLock = new object();
        static bool m_Inited;

        IntPtr m_VHacd;

        [DllImport("vhacd", EntryPoint = "_ZN5VHACD11CreateVHACDEv")]
        static extern IntPtr VHacd_Create();

        [DllImport("vhacd", EntryPoint = "vhacd_Cancel")]
        static extern void VHacd_Cancel(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_Release")]
        static extern void VHacd_Release(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_Clean")]
        static extern void VHacd_Clean(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_GetNConvexHulls")]
        static extern int VHacd_GetNConvexHulls(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_Compute")]
        static extern bool VHacd_Compute(IntPtr vhacd, double[] points, uint stridePoints, uint countPoints, int[] triangles, uint strideTriangles, uint countTriangles, ref Parameters param);

        [DllImport("vhacd", EntryPoint = "vhacd_GetConvexHull")]
        static extern void VHacd_GetConvexHull(IntPtr vhacd, uint index, ref ConvexHull ch);

        public void Dispose()
        {
            if(m_VHacd != IntPtr.Zero)
            {
                VHacd_Release(m_VHacd);
                m_VHacd = IntPtr.Zero;
            }
        }


        public VHACD()
        {
            m_VHacd = VHacd_Create();
            if(m_VHacd == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
        }

        public PhysicsConvexShape Compute(MeshLOD m)
        {
            if(m_VHacd == IntPtr.Zero)
            {
                throw new ObjectDisposedException("VHACD");
            }
            double[] points = new double[m.Vertices.Count * 3];
            int[] tris = new int[m.Triangles.Count * 3];
            int idx = 0;
            foreach(Vector3 v in m.Vertices)
            {
                points[idx++] = v.X;
                points[idx++] = v.Y;
                points[idx++] = v.Z;
            }

            idx = 0;
            foreach(Triangle t in m.Triangles)
            {
                tris[idx++] = t.Vertex1;
                tris[idx++] = t.Vertex2;
                tris[idx++] = t.Vertex3;
            }

            Parameters p = new Parameters();
            if(!VHacd_Compute(m_VHacd, points, 3, (uint)m.Vertices.Count, tris, 3, (uint)m.Triangles.Count, ref p))
            {
                throw new InvalidDataException();
            }

            PhysicsConvexShape shape = new PhysicsConvexShape();
            for(uint hullidx = 0; hullidx < VHacd_GetNConvexHulls(m_VHacd); ++hullidx)
            {
                ConvexHull hull = new ConvexHull();
                VHacd_GetConvexHull(m_VHacd, hullidx, ref hull);
                double[] resPoints = new double[hull.NumPoints];
                Marshal.Copy(hull.Points, resPoints, 0, hull.NumPoints * 3);
                int[] resTris = new int[hull.NumTriangles];
                Marshal.Copy(hull.Triangles, resTris, 0, hull.NumTriangles * 3);

                PhysicsConvexShape.ConvexHull cHull = new PhysicsConvexShape.ConvexHull();
                for (int vertidx = 0; vertidx < hull.NumPoints; vertidx += 3)
                {
                    cHull.Vertices.Add(new Vector3(
                        resPoints[vertidx + 0],
                        resPoints[vertidx + 1],
                        resPoints[vertidx + 2]));
                }
                for(int triidx = 0; triidx < hull.NumTriangles; ++triidx)
                {
                    cHull.Triangles.Add(resTris[triidx]);
                }
                shape.Hulls.Add(cHull);
            }

            return shape;
        }

        static VHACD()
        {
            if (!m_Inited)
            {
                lock (m_InitLock)
                {
                    if (!m_Inited)
                    {
                        /* preload necessary windows dll */
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            if (Environment.Is64BitProcess)
                            {
                                if (IntPtr.Zero == LoadLibrary(Path.GetFullPath("platform-libs/windows/64/vhacd.dll")))
                                {
                                    throw new FileNotFoundException("missing platform-libs/windows/64/vhacd.dll");
                                }
                            }
                            else
                            {
                                if (IntPtr.Zero == LoadLibrary(Path.GetFullPath("platform-libs/windows/32/vhacd.dll")))
                                {
                                    throw new FileNotFoundException("missing platform-libs/windows/32/vhacd.dll");
                                }
                            }
                        }
                        m_Inited = true;
                    }
                }
            }
        }
    }
}
