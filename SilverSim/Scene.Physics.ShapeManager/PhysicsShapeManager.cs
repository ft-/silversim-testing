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
using System;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Scene.Physics.ShapeManager
{
    /** <summary>PhysicsShapeManager provides a common accessor to Convex Hull generation from primitives</summary> */
    [Description("Physics Shape Manager")]
    [PluginName("PhysicsShapeManager")]
    public sealed class PhysicsShapeManager : IPlugin, IPhysicsHacdCleanCache
    {
        private static readonly ILog m_Log = LogManager.GetLogger("PHYSICS SHAPE MANAGER");
        private AssetServiceInterface m_AssetService;
        private SimulationDataStorageInterface m_SimulationStorage;

        private string GetMeshKey(UUID meshid, PrimitivePhysicsShapeType physicsShape)
        {
            return string.Format("{0}-{1}", meshid, (int)physicsShape);
        }

        private readonly RwLockedDictionary<string, PhysicsConvexShape> m_ConvexShapesBySculptMesh = new RwLockedDictionary<string, PhysicsConvexShape>();
        private readonly RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape> m_ConvexShapesByPrimShape = new RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape>();

        private readonly string m_AssetServiceName;
        private readonly string m_SimulationDataStorageName;
        private readonly ReaderWriterLock m_Lock = new ReaderWriterLock();

        /** <summary>referencing class to provide usage counting for meshes</summary> */
        public sealed class PhysicsShapeMeshReference : PhysicsShapeReference
        {
            private readonly UUID m_ID;
            private readonly PrimitivePhysicsShapeType m_ShapeType;
            internal PhysicsShapeMeshReference(UUID id, PrimitivePhysicsShapeType shapeType, PhysicsShapeManager manager, PhysicsConvexShape shape)
                : base(manager, shape)
            {
                m_ID = id;
                m_ShapeType = shapeType;
            }

            ~PhysicsShapeMeshReference()
            {
                m_PhysicsManager.DecrementUseCount(m_ID, m_ShapeType, m_ConvexShape);
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

        private readonly bool m_DisableCache;

        public PhysicsShapeManager(
            IConfig ownSection)
        {
            m_AssetServiceName = ownSection.GetString("AssetService");
            m_SimulationDataStorageName = ownSection.GetString("SimulationDataStorage");
            m_DisableCache = ownSection.GetBoolean("DisableHacdCache", false);
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
            if(m_DisableCache)
            {
                loader.KnownConfigurationIssues.Add("HACD cache is disabled");
            }
            m_AssetService = loader.GetService<AssetServiceInterface>(m_AssetServiceName);
            m_SimulationStorage = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataStorageName);
            DefaultAvatarConvexShape = new PhysicsShapeDefaultAvatarReference(this, GenerateDefaultAvatarShape());
        }

        private static PhysicsConvexShape DecomposeConvex(MeshLOD lod, bool useSingleConvex = false)
        {
            using (var vhacd = new VHACD())
            {
                return vhacd.Compute(lod, useSingleConvex);
            }
        }

        internal void DecrementUseCount(UUID id, PrimitivePhysicsShapeType physicsShape, PhysicsConvexShape shape)
        {
            if (0 == Interlocked.Decrement(ref shape.UseCount))
            {
                m_Lock.AcquireWriterLock(() => m_ConvexShapesBySculptMesh.RemoveIf(GetMeshKey(id, physicsShape), (PhysicsConvexShape s) => s.UseCount == 0));
            }
        }

        internal void DecrementUseCount(ObjectPart.PrimitiveShape primshape, PhysicsConvexShape shape)
        {
            if (0 == Interlocked.Decrement(ref shape.UseCount))
            {
                m_Lock.AcquireWriterLock(() => m_ConvexShapesByPrimShape.RemoveIf(primshape, (PhysicsConvexShape s) => s.UseCount == 0));
            }
        }

        public bool TryGetConvexShape(PrimitivePhysicsShapeType physicsShape, ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physics)
        {
            if(physicsShape == PrimitivePhysicsShapeType.None)
            {
                physics = null;
                return false;
            }
            else if(shape.Type == PrimitiveShapeType.Sculpt)
            {
                switch(shape.SculptType)
                {
                    case PrimitiveSculptType.Mesh:
                        return TryGetConvexShapeFromMesh(physicsShape, shape, out physics);

                    default:
                        /* calculate convex from sculpt */
                        return TryGetConvexShapeFromPrim(physicsShape, shape, out physics);
                }
            }
            else
            {
                /* calculate convex from prim mesh */
                return TryGetConvexShapeFromPrim(physicsShape, shape, out physics);
            }
        }

        private PhysicsConvexShape ConvertToMesh(PrimitivePhysicsShapeType physicsShape, ObjectPart.PrimitiveShape shape)
        {
            PhysicsConvexShape convexShape = null;
            bool hasHullList = false;
            if (shape.Type == PrimitiveShapeType.Sculpt && shape.SculptType == PrimitiveSculptType.Mesh)
            {
                var m = new LLMesh(m_AssetService[shape.SculptMap]);
                if(physicsShape == PrimitivePhysicsShapeType.Convex)
                {
#if DEBUG
                    m_Log.DebugFormat("Selected convex of {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                    if (m.HasConvexPhysics())
                    {
                        try
                        {
#if DEBUG
                            m_Log.DebugFormat("Using convex of {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                            convexShape = m.GetConvexPhysics(false);
                            hasHullList = convexShape.HasHullList;
                            return convexShape;
                        }
                        catch(NoSuchMeshDataException)
                        {
                            /* no shape */
#if DEBUG
                            m_Log.DebugFormat("No convex in asset of {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                        }
                        catch (Exception e)
                        {
                            m_Log.Warn($"Failed to get convex data of {shape.SculptType} {shape.SculptMap}", e);
                        }
                    }
#if DEBUG
                    else
                    {
                        m_Log.DebugFormat("No convex shape in {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
                    }
#endif
                    if (convexShape == null)
                    {
#if DEBUG
                        m_Log.DebugFormat("Using decompose to single convex for {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                        MeshLOD lod = m.GetLOD(LLMesh.LodLevel.LOD3);
                        lod.Optimize();
                        convexShape = DecomposeConvex(lod, true);
                    }
                }
                else
                {
#if DEBUG
                    m_Log.DebugFormat("Selected detailed physics of {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                    if (m.HasLOD(LLMesh.LodLevel.Physics))
                    {
                        /* check for physics mesh before giving out the single hull */
#if DEBUG
                        m_Log.DebugFormat("Using detailed physics of {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                        MeshLOD lod = m.GetLOD(LLMesh.LodLevel.Physics);
                        lod.Optimize();
                        convexShape = DecomposeConvex(lod);
                    }
                    else if(m.HasConvexPhysics())
                    {
#if DEBUG
                        m_Log.DebugFormat("Using convex of {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                        try
                        {
                            convexShape = m.GetConvexPhysics(true);
                            hasHullList = convexShape.HasHullList;
                        }
                        catch(NoSuchMeshDataException)
                        {
                            /* no shape */
#if DEBUG
                            m_Log.DebugFormat("No suitable convex in asset of {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                        }
                        catch(Exception e)
                        {
                            m_Log.Warn($"Failed to get convex data of {shape.Type}/{shape.SculptType}/{shape.SculptMap}", e);
                        }
                        if (convexShape == null)
                        {
                            /* this way we keep convex hull type functional by having it only get active on PrimitivePhysicsShapeType.Prim */
#if DEBUG
                            m_Log.DebugFormat("Using decompose to convex for {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                            MeshLOD lod = m.GetLOD(LLMesh.LodLevel.LOD3);
                            lod.Optimize();
                            convexShape = DecomposeConvex(lod);
                        }
                    }
                }
            }
            else
            {
#if DEBUG
                m_Log.DebugFormat("Using decompose to convex for {0}/{1}/{2}", shape.Type, shape.SculptType, shape.SculptMap);
#endif
                MeshLOD m = shape.ToMesh(m_AssetService);
                m.Optimize();
                convexShape = DecomposeConvex(m, physicsShape == PrimitivePhysicsShapeType.Convex);
            }

            return convexShape;
        }

        private bool TryGetConvexShapeFromMesh(PrimitivePhysicsShapeType physicsShape, ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physicshaperef)
        {
            PhysicsConvexShape physicshape;
            PhysicsShapeReference physicshaperes = null;
            UUID meshId = shape.SculptMap;
            bool s = m_Lock.AcquireReaderLock(() =>
            {
                if (m_ConvexShapesBySculptMesh.TryGetValue(GetMeshKey(meshId, physicsShape), out physicshape))
                {
                    physicshaperes = new PhysicsShapeMeshReference(meshId, physicsShape, this, physicshape);
                    return true;
                }
                return false;
            });
            if(s)
            {
                physicshaperef = physicshaperes;
                return true;
            }

            if (m_DisableCache || !m_SimulationStorage.PhysicsConvexShapes.TryGetValue(meshId, physicsShape, out physicshape))
            {
                /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
                physicshape = ConvertToMesh(physicsShape, shape);
                if(physicshape == null)
                {
                    physicshaperef = null;
                    return false;
                }
                foreach (PhysicsConvexShape.ConvexHull hull in physicshape.Hulls)
                {
                    if (hull.Vertices.Count == 0)
                    {
                        m_Log.WarnFormat("Physics shape of mesh generated a 0 point hull: {0} / {1}", physicsShape, meshId);
                        physicshaperef = null;
                        return false;
                    }
                }
                m_SimulationStorage.PhysicsConvexShapes[meshId, physicsShape] = physicshape;
            }

            /* we only lock out the decrement use count here */
            physicshaperef = m_Lock.AcquireReaderLock(() =>
            {
                try
                {
                    m_ConvexShapesBySculptMesh.Add(GetMeshKey(meshId, physicsShape), physicshape);
                }
                catch
                {
                    physicshape = m_ConvexShapesBySculptMesh[GetMeshKey(meshId, physicsShape)];
                }
                return new PhysicsShapeMeshReference(meshId, physicsShape, this, physicshape);
            });

            return true;
        }

        private bool TryGetConvexShapeFromPrim(PrimitivePhysicsShapeType physicsShape, ObjectPart.PrimitiveShape shape, out PhysicsShapeReference physicshaperef)
        {
            PhysicsConvexShape physicshape;
            PhysicsShapeReference physicshaperes = null;
            bool s = m_Lock.AcquireReaderLock(() =>
            {
                if (m_ConvexShapesByPrimShape.TryGetValue(shape, out physicshape))
                {
                    physicshaperes = new PhysicsShapePrimShapeReference(shape, this, physicshape);
                    return true;
                }
                return false;
            });
            if(s)
            {
                physicshaperef = physicshaperes;
                return true;
            }

            if (m_DisableCache || !m_SimulationStorage.PhysicsConvexShapes.TryGetValue(shape, out physicshape))
            {
                /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
                physicshape = ConvertToMesh(physicsShape, shape);
                if(physicshape == null)
                {
                    physicshaperef = null;
                    return false;
                }
                foreach(PhysicsConvexShape.ConvexHull hull in physicshape.Hulls)
                {
                    if(hull.Vertices.Count == 0)
                    {
                        if (shape.Type == PrimitiveShapeType.Sculpt)
                        {
                            m_Log.WarnFormat("Physics shape of sculpt generated a 0 point hull: {0} / {1}", physicsShape, shape.Serialization.ToHexString());
                        }
                        else
                        {
                            m_Log.WarnFormat("Physics shape of prim generated a 0 point hull: {0} / {1} / {2} / {3}", physicsShape, shape.PCode, shape.Type, shape.Serialization.ToHexString());
                        }
                        physicshaperef = null;
                        return false;
                    }
                }
                m_SimulationStorage.PhysicsConvexShapes[shape] = physicshape;
            }

            /* we only lock out the decrement use count here */
            physicshaperef = m_Lock.AcquireReaderLock(() =>
            {
                try
                {
                    m_ConvexShapesByPrimShape.Add(shape, physicshape);
                }
                catch
                {
                    physicshape = m_ConvexShapesByPrimShape[shape];
                }
                return new PhysicsShapePrimShapeReference(shape, this, physicshape);
            });

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