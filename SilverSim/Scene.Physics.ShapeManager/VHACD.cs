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

using log4net;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SilverSim.Scene.Physics.ShapeManager
{
    public class VHACD : IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("VHACD");

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
                Concavity = 0.0025;
                PlaneDownsampling = 4;
                ConvexhullDownsampling = 4;
                Alpha = 0.05;
                Beta = 0.05;
                Gamma = 0.00125;
                Pca = 0;
                Mode = 0; // 0: voxel-based (recommended), 1: tetrahedron-based
                MaxNumVerticesPerCH = 32;
                MinVolumePerCH = 0.0001;
                Callback = IntPtr.Zero;
                Logger = IntPtr.Zero;
                ConvexhullApproximation = true;
                OclAcceleration = true;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        private static readonly object m_InitLock = new object();
        private static readonly bool m_Inited;

        private IntPtr m_VHacd;

        [DllImport("vhacd", EntryPoint = "_ZN5VHACD11CreateVHACDEv")]
        private static extern IntPtr VHacd_Create();

        [DllImport("vhacd", EntryPoint = "vhacd_Cancel")]
        private static extern void VHacd_Cancel(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_Release")]
        private static extern void VHacd_Release(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_Clean")]
        private static extern void VHacd_Clean(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_GetNConvexHulls")]
        private static extern int VHacd_GetNConvexHulls(IntPtr vhacd);

        [DllImport("vhacd", EntryPoint = "vhacd_Compute")]
        private static extern bool VHacd_Compute(IntPtr vhacd, double[] points, uint stridePoints, uint countPoints, int[] triangles, uint strideTriangles, uint countTriangles, ref Parameters param);

        [DllImport("vhacd", EntryPoint = "vhacd_GetConvexHull")]
        private static extern void VHacd_GetConvexHull(IntPtr vhacd, uint index, ref ConvexHull ch);

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
            var points = new double[m.Vertices.Count * 3];
            var tris = new int[m.Triangles.Count * 3];
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

            var p = new Parameters();
            if(!VHacd_Compute(m_VHacd, points, 3, (uint)m.Vertices.Count, tris, 3, (uint)m.Triangles.Count, ref p))
            {
                throw new InvalidDataException();
            }

            var shape = new PhysicsConvexShape();
            int numhulls = VHacd_GetNConvexHulls(m_VHacd);
            for (uint hullidx = 0; hullidx < numhulls; ++hullidx)
            {
                var hull = new ConvexHull();
                VHacd_GetConvexHull(m_VHacd, hullidx, ref hull);
                var resPoints = new double[hull.NumPoints * 3];
                Marshal.Copy(hull.Points, resPoints, 0, hull.NumPoints * 3);
                var resTris = new int[hull.NumTriangles * 3];
                Marshal.Copy(hull.Triangles, resTris, 0, hull.NumTriangles * 3);

                var cHull = new PhysicsConvexShape.ConvexHull();
                for (int vertidx = 0; vertidx < hull.NumPoints * 3; vertidx += 3)
                {
                    cHull.Vertices.Add(new Vector3(
                        resPoints[vertidx + 0],
                        resPoints[vertidx + 1],
                        resPoints[vertidx + 2]));
                }

                int vCount = cHull.Vertices.Count;
                for(int triidx = 0; triidx < hull.NumTriangles * 3; ++triidx)
                {
                    int tri = resTris[triidx];
                    if(tri >= vCount || tri < 0)
                    {
                        m_Log.ErrorFormat("Tri Index out of range");
                        throw new InvalidDataException("Tri index out of range");
                    }
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
                        string installationBinPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        /* preload necessary windows dll */
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            if (Environment.Is64BitProcess)
                            {
                                if (IntPtr.Zero == LoadLibrary(Path.Combine(installationBinPath, "../platform-libs/windows/64/vhacd.dll")))
                                {
                                    throw new FileNotFoundException("missing platform-libs/windows/64/vhacd.dll");
                                }
                            }
                            else
                            {
                                if (IntPtr.Zero == LoadLibrary(Path.Combine(installationBinPath, "../platform-libs/windows/32/vhacd.dll")))
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
