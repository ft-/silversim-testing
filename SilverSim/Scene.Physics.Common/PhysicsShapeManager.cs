// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Mesh;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scene.Physics.Common
{
    public class PhysicsShapeManager
    {
        readonly AssetServiceInterface m_AssetService;
        readonly SimulationDataStorageInterface m_SimulationStorage;

        readonly RwLockedDictionary<UUID, PhysicsConvexShape> m_ConvexShapesBySculptMesh = new RwLockedDictionary<UUID, PhysicsConvexShape>();
        readonly RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape> m_ConvexShapesByPrimShape = new RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape>();

        public PhysicsShapeManager(
            AssetServiceInterface assetService,
            SimulationDataStorageInterface simulationStorage)
        {
            m_AssetService = assetService;
            m_SimulationStorage = simulationStorage;
        }

        PhysicsConvexShape DecomposeConvex(MeshLOD lod)
        {
#warning Implement Convex Decomposition
            throw new NotImplementedException("DecomposeConvex");
        }

        public bool TryGetConvexShape(ObjectPart.PrimitiveShape shape, out PhysicsConvexShape physics)
        {
            if(shape.Type == PrimitiveShapeType.Sculpt)
            {
                switch(shape.SculptType)
                {
                    case PrimitiveSculptType.Mesh:
                        return TryGetConvexShapeFromMesh(shape, out physics);

                    default:
                        /* calculate convex from sculpt */
                        return TryGetConvexShapeFromSculpt(shape, out physics);
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

        bool TryGetConvexShapeFromSculpt(ObjectPart.PrimitiveShape shape, out PhysicsConvexShape physics)
        {
            if (m_ConvexShapesByPrimShape.TryGetValue(shape, out physics))
            {
                return true;
            }

            /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
            physics = ConvertToMesh(shape);
            try
            {
                m_ConvexShapesByPrimShape.Add(shape, physics);
            }
            catch
            {
                physics = m_ConvexShapesByPrimShape[shape];
            }

            m_SimulationStorage.PhysicsConvexShapes[shape] = physics;

            return true;
        }

        bool TryGetConvexShapeFromMesh(ObjectPart.PrimitiveShape shape, out PhysicsConvexShape physics)
        {
            UUID meshId = shape.SculptMap;
            if(m_ConvexShapesBySculptMesh.TryGetValue(meshId, out physics))
            {
                return true;
            }

            /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
            physics = ConvertToMesh(shape);
            try
            {
                m_ConvexShapesBySculptMesh.Add(meshId, physics);
            }
            catch
            {
                physics = m_ConvexShapesBySculptMesh[meshId];
            }

            m_SimulationStorage.PhysicsConvexShapes[meshId] = physics;

            return true;
        }

        bool TryGetConvexShapeFromPrim(ObjectPart.PrimitiveShape shape, out PhysicsConvexShape physics)
        {
            if (m_ConvexShapesByPrimShape.TryGetValue(shape, out physics))
            {
                return true;
            }

            /* we may produce additional meshes sometimes but it is better not to lock while generating the mesh */
            physics = ConvertToMesh(shape);
            try
            {
                m_ConvexShapesByPrimShape.Add(shape, physics);
            }
            catch
            {
                physics = m_ConvexShapesByPrimShape[shape];
            }

            m_SimulationStorage.PhysicsConvexShapes[shape] = physics;

            return true;
        }
    }
}