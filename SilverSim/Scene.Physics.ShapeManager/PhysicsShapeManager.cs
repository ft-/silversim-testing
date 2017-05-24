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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Mesh;
using SilverSim.Scene.Types.Physics;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Scene.Physics.ShapeManager
{
    /** <summary>PhysicsShapeManager provides a common accessor to Convex Hull generation from primitives</summary> */
    [Description("Physics Shape Manager")]
    [PluginName("PhysicsShapeManager")]
    public sealed class PhysicsShapeManager : IPlugin, IPhysicsHacdCleanCache
    {
        private AssetServiceInterface m_AssetService;
        private SimulationDataStorageInterface m_SimulationStorage;

        private readonly RwLockedDictionary<UUID, PhysicsConvexShape> m_ConvexShapesBySculptMesh = new RwLockedDictionary<UUID, PhysicsConvexShape>();
        private readonly RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape> m_ConvexShapesByPrimShape = new RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape>();

        private readonly string m_AssetServiceName;
        private readonly string m_SimulationDataStorageName;
        private readonly ReaderWriterLock m_Lock = new ReaderWriterLock();

        /** <summary>referencing class to provide usage counting for meshes</summary> */
        public sealed class PhysicsShapeMeshReference : PhysicsShapeReference
        {
            private readonly UUID m_ID;
            internal PhysicsShapeMeshReference(UUID id, PhysicsShapeManager manager, PhysicsConvexShape shape)
                : base(manager, shape)
            {
                m_ID = id;
            }

            ~PhysicsShapeMeshReference()
            {
                m_PhysicsManager.DecrementUseCount(m_ID, m_ConvexShape);
            }
        }

        /** <summary>referencing class to provide default avatar shapehes</summary> */
        public sealed class PhysicsShapeDefaultAvatarReference : PhysicsShapeReference
        {
            internal PhysicsShapeDefaultAvatarReference(PhysicsShapeManager manager, PhysicsConvexShape shape)
                : base(manager, shape)
            {
            }
        }

        /** <summary>referencing class to provide usage counting for sculpts and prims</summary> */
        public sealed class PhysicsShapePrimShapeReference : PhysicsShapeReference
        {
            private readonly ObjectPart.PrimitiveShape m_Shape;

            internal PhysicsShapePrimShapeReference(ObjectPart.PrimitiveShape primshape, PhysicsShapeManager manager, PhysicsConvexShape shape)
                : base(manager, shape)
            {
                m_Shape = new ObjectPart.PrimitiveShape(primshape);
            }

            ~PhysicsShapePrimShapeReference()
            {
                m_PhysicsManager.DecrementUseCount(m_Shape, m_ConvexShape);
            }
        }

        public PhysicsShapeManager(
            IConfig ownSection)
        {
            m_AssetServiceName = ownSection.GetString("AssetService");
            m_SimulationDataStorageName = ownSection.GetString("SimulationDataStorage");
        }

        private static PhysicsConvexShape GenerateDefaultAvatarShape()
        {
            var meshLod = new MeshLOD();

            meshLod.Vertices.Add(new Vector3(-0.5, -0.5, 0));
            meshLod.Vertices.Add(new Vector3(0.5, -0.5, 0));
            meshLod.Vertices.Add(new Vector3(0.5, 0.5, 0));
            meshLod.Vertices.Add(new Vector3(-0.5, 0.5, 0));

            meshLod.Vertices.Add(new Vector3(-0.1, -0.1, -0.5));
            meshLod.Vertices.Add(new Vector3(0.1, -0.1, -0.5));
            meshLod.Vertices.Add(new Vector3(0.1, 0.1, -0.5));
            meshLod.Vertices.Add(new Vector3(-0.1, 0.1, -0.5));

            meshLod.Vertices.Add(new Vector3(-0.5, -0.5, 0.5));
            meshLod.Vertices.Add(new Vector3(0.5, -0.5, 0.5));
            meshLod.Vertices.Add(new Vector3(0.5, 0.5, 0.5));
            meshLod.Vertices.Add(new Vector3(-0.5, 0.5, 0.5));

            #region Top
            meshLod.Triangles.Add(new Triangle(8, 9, 10));
            meshLod.Triangles.Add(new Triangle(11, 8, 10));
            #endregion

            #region Bottom
            meshLod.Triangles.Add(new Triangle(5, 4, 6));
            meshLod.Triangles.Add(new Triangle(6, 4, 7));
            #endregion

            #region Lower Sides A
            meshLod.Triangles.Add(new Triangle(1, 4, 5));
            meshLod.Triangles.Add(new Triangle(1, 0, 4));
            #endregion

            #region Lower Sides B
            meshLod.Triangles.Add(new Triangle(1, 5, 6));
            meshLod.Triangles.Add(new Triangle(2, 1, 6));
            #endregion

            #region Lower Sides C
            meshLod.Triangles.Add(new Triangle(2, 6, 7));
            meshLod.Triangles.Add(new Triangle(3, 2, 7));
            #endregion

            #region Lower Sides D
            meshLod.Triangles.Add(new Triangle(4, 3, 7));
            meshLod.Triangles.Add(new Triangle(4, 0, 3));
            #endregion

            #region Upper Sides A
            meshLod.Triangles.Add(new Triangle(0, 1, 8));
            meshLod.Triangles.Add(new Triangle(8, 1, 9));
            #endregion

            #region Upper Sides B
            meshLod.Triangles.Add(new Triangle(1, 2, 9));
            meshLod.Triangles.Add(new Triangle(9, 2, 10));
            #endregion

            #region Upper Sides C
            meshLod.Triangles.Add(new Triangle(2, 3, 10));
            meshLod.Triangles.Add(new Triangle(10, 3, 11));
            #endregion

            #region Upper Sides D
            meshLod.Triangles.Add(new Triangle(3, 0, 8));
            meshLod.Triangles.Add(new Triangle(3, 8, 11));
            #endregion

            return DecomposeConvex(meshLod);
        }

        public PhysicsShapeReference DefaultAvatarConvexShape { get; private set;}

        public void Startup(ConfigurationLoader loader)
        {
            m_AssetService = loader.GetService<AssetServiceInterface>(m_AssetServiceName);
            m_SimulationStorage = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataStorageName);
            DefaultAvatarConvexShape = new PhysicsShapeDefaultAvatarReference(this, GenerateDefaultAvatarShape());
        }

        private static PhysicsConvexShape DecomposeConvex(MeshLOD lod)
        {
            using (var vhacd = new VHACD())
            {
                return vhacd.Compute(lod);
            }
        }

        internal void DecrementUseCount(UUID id, PhysicsConvexShape shape)
        {
            if (0 == Interlocked.Decrement(ref shape.UseCount))
            {
                m_Lock.AcquireWriterLock(-1);
                try
                {
                    m_ConvexShapesBySculptMesh.RemoveIf(id, (PhysicsConvexShape s) => s.UseCount == 0);
                }
                finally
                {
                    m_Lock.ReleaseWriterLock();
                }
            }
        }

        internal void DecrementUseCount(ObjectPart.PrimitiveShape primshape, PhysicsConvexShape shape)
        {
            if (0 == Interlocked.Decrement(ref shape.UseCount))
            {
                m_Lock.AcquireWriterLock(-1);
                try
                {
                    m_ConvexShapesByPrimShape.RemoveIf(primshape, (PhysicsConvexShape s) => s.UseCount == 0);
                }
                finally
                {
                    m_Lock.ReleaseWriterLock();
                }
            }
        }

        public bool TryGetConvexShape(ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physics)
        {
            if(shape.Type == PrimitiveShapeType.Sculpt)
            {
                switch(shape.SculptType)
                {
                    case PrimitiveSculptType.Mesh:
                        return TryGetConvexShapeFromMesh(shape, out physics);

                    default:
                        /* calculate convex from sculpt */
                        return TryGetConvexShapeFromPrim(shape, out physics);
                }
            }
            else
            {
                /* calculate convex from prim mesh */
                return TryGetConvexShapeFromPrim(shape, out physics);
            }
        }

        private PhysicsConvexShape ConvertToMesh(ObjectPart.PrimitiveShape shape)
        {
            PhysicsConvexShape convexShape = null;
            if (shape.Type == PrimitiveShapeType.Sculpt && shape.SculptType == PrimitiveSculptType.Mesh)
            {
                var m = new LLMesh(m_AssetService[shape.SculptMap]);
                if(m.HasConvexPhysics())
                {
                    convexShape = m.GetConvexPhysics();
                }
                if(convexShape?.HasHullList == false && m.HasLOD(LLMesh.LodLevel.Physics))
                {
                    /* check for physics mesh before giving out the single hull */
                    MeshLOD lod = m.GetLOD(LLMesh.LodLevel.Physics);
                    lod.Optimize();
                    convexShape = DecomposeConvex(lod);
                }

                if(convexShape == null)
                {
                    /* go for visual LODs */
                    MeshLOD lod;
                    if(m.HasLOD(LLMesh.LodLevel.LOD0))
                    {
                        lod = m.GetLOD(LLMesh.LodLevel.LOD0);
                    }
                    else if (m.HasLOD(LLMesh.LodLevel.LOD1))
                    {
                        lod = m.GetLOD(LLMesh.LodLevel.LOD1);
                    }
                    else if (m.HasLOD(LLMesh.LodLevel.LOD2))
                    {
                        lod = m.GetLOD(LLMesh.LodLevel.LOD2);
                    }
                    else
                    {
                        lod = m.GetLOD(LLMesh.LodLevel.LOD3);
                    }

                    convexShape = DecomposeConvex(lod);
                }
            }
            else
            {
                MeshLOD m = shape.ToMesh(m_AssetService);
                m.Optimize();

                convexShape = DecomposeConvex(m);
            }

            return convexShape;
        }

        private bool TryGetConvexShapeFromMesh(ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physicshaperef)
        {
            PhysicsConvexShape physicshape;
            UUID meshId = shape.SculptMap;
            m_Lock.AcquireReaderLock(-1);
            try
            {
                if (m_ConvexShapesBySculptMesh.TryGetValue(meshId, out physicshape))
                {
                    physicshaperef = new PhysicsShapeMeshReference(meshId, this, physicshape);
                    return true;
                }
            }
            finally
            {
                m_Lock.ReleaseReaderLock();
            }

            if (!m_SimulationStorage.PhysicsConvexShapes.TryGetValue(meshId, out physicshape))
            {
                /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
                physicshape = ConvertToMesh(shape);
                m_SimulationStorage.PhysicsConvexShapes[meshId] = physicshape;
            }

            m_Lock.AcquireReaderLock(-1);
            try
            {
                try
                {
                    m_ConvexShapesBySculptMesh.Add(meshId, physicshape);
                }
                catch
                {
                    physicshape = m_ConvexShapesBySculptMesh[meshId];
                }
                physicshaperef = new PhysicsShapeMeshReference(meshId, this, physicshape);
            }
            finally
            {
                m_Lock.ReleaseReaderLock();
            }

            return true;
        }

        private bool TryGetConvexShapeFromPrim(ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physicshaperef)
        {
            PhysicsConvexShape physicshape;
            m_Lock.AcquireReaderLock(-1);
            try
            {
                if (m_ConvexShapesByPrimShape.TryGetValue(shape, out physicshape))
                {
                    physicshaperef = new PhysicsShapePrimShapeReference(shape, this, physicshape);
                    return true;
                }
            }
            finally
            {
                m_Lock.ReleaseReaderLock();
            }

            if (!m_SimulationStorage.PhysicsConvexShapes.TryGetValue(shape, out physicshape))
            {
                /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
                physicshape = ConvertToMesh(shape);
                m_SimulationStorage.PhysicsConvexShapes[shape] = physicshape;
            }

            m_Lock.AcquireReaderLock(-1);
            try
            {
                try
                {
                    m_ConvexShapesByPrimShape.Add(shape, physicshape);
                }
                catch
                {
                    physicshape = m_ConvexShapesByPrimShape[shape];
                }
                physicshaperef = new PhysicsShapePrimShapeReference(shape, this, physicshape);
            }
            finally
            {
                m_Lock.ReleaseReaderLock();
            }

            return true;
        }

        void IPhysicsHacdCleanCache.CleanCache()
        {
            m_ConvexShapesBySculptMesh.Clear();
            m_ConvexShapesByPrimShape.Clear();
        }

        HacdCleanCacheOrder IPhysicsHacdCleanCache.CleanOrder => HacdCleanCacheOrder.PhysicsShapeManager;
    }
}