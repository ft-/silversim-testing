// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Mesh;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System.Threading;

namespace SilverSim.Scene.Physics.ShapeManager
{
    public sealed class PhysicsShapeManager : IPlugin
    {
        AssetServiceInterface m_AssetService;
        SimulationDataStorageInterface m_SimulationStorage;

        readonly RwLockedDictionary<UUID, PhysicsConvexShape> m_ConvexShapesBySculptMesh = new RwLockedDictionary<UUID, PhysicsConvexShape>();
        readonly RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape> m_ConvexShapesByPrimShape = new RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape>();

        string m_AssetServiceName;
        string m_SimulationDataStorageName;
        ReaderWriterLock m_Lock = new ReaderWriterLock();

        public sealed class PhysicsShapeMeshReference : PhysicsShapeReference
        {
            UUID m_ID;
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

        public sealed class PhysicsShapePrimShapeReference : PhysicsShapeReference
        {
            ObjectPart.PrimitiveShape m_Shape;

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
            string assetServiceName,
            string simulationStorageName)
        {
            m_AssetServiceName = assetServiceName;
            m_SimulationDataStorageName = simulationStorageName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_AssetService = loader.GetService<AssetServiceInterface>(m_AssetServiceName);
            m_SimulationStorage = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataStorageName);
        }

        PhysicsConvexShape DecomposeConvex(MeshLOD lod)
        {
            using (VHACD vhacd = new VHACD())
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
                    m_ConvexShapesBySculptMesh.RemoveIf(id, delegate (PhysicsConvexShape s)
                    {
                        return s.UseCount == 0;
                    });
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
                    m_ConvexShapesByPrimShape.RemoveIf(primshape, delegate (PhysicsConvexShape s)
                    {
                        return s.UseCount == 0;
                    });
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

        PhysicsConvexShape ConvertToMesh(ObjectPart.PrimitiveShape shape)
        {
            PhysicsConvexShape convexShape = null;
            if (shape.Type == PrimitiveShapeType.Sculpt && shape.SculptType == PrimitiveSculptType.Mesh)
            {
                LLMesh m = new LLMesh(m_AssetService[shape.SculptMap]);
                if(m.HasConvexPhysics())
                {
                    convexShape = m.GetConvexPhysics();
                }
                if(null != convexShape && !convexShape.HasHullList)
                {
                    /* check for physics mesh before giving out the single hull */
                    if(m.HasLOD(LLMesh.LodLevel.Physics))
                    {
                        MeshLOD lod = m.GetLOD(LLMesh.LodLevel.Physics);
                        lod.Optimize();
                        convexShape = DecomposeConvex(lod);
                    }
                }

                if(null == convexShape)
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

        bool TryGetConvexShapeFromMesh(ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physicshaperef)
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

            /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
            physicshape = ConvertToMesh(shape);

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

        bool TryGetConvexShapeFromPrim(ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physicshaperef)
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

            /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
            physicshape = ConvertToMesh(shape);

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
    }

    [PluginName("PhysicsShapeManager")]
    public class PhysicsShapeManagerFactory : IPluginFactory
    {
        public PhysicsShapeManagerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new PhysicsShapeManager(
                ownSection.GetString("AssetService"),
                ownSection.GetString("SimulationDataStorage"));
        }
    }
}