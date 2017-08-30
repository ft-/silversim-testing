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

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;

namespace SilverSim.Database.Memory.SimulationData
{
    partial class MemorySimulationDataStorage : ISimulationDataPhysicsConvexStorageInterface, IPhysicsHacdCleanCache
    {
        private static string GetMeshKey(UUID meshid, PrimitivePhysicsShapeType physicsShape)
        {
            return string.Format("{0}-{1}", meshid, (int)physicsShape);
        }
        private readonly RwLockedDictionary<string, PhysicsConvexShape> m_ConvexShapesByMesh = new RwLockedDictionary<string, PhysicsConvexShape>();
        private readonly RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape> m_ConvexShapesByPrimShape = new RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape>();

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[UUID meshid, PrimitivePhysicsShapeType physicsShape]
        {
            get { return m_ConvexShapesByMesh[GetMeshKey(meshid, physicsShape)]; }

            set { m_ConvexShapesByMesh[GetMeshKey(meshid, physicsShape)] = value; }
        }

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[ObjectPart.PrimitiveShape primShape]
        {
            get { return m_ConvexShapesByPrimShape[primShape]; }

            set { m_ConvexShapesByPrimShape[primShape] = value; }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(UUID meshid, PrimitivePhysicsShapeType physicsShape, out PhysicsConvexShape shape) =>
            m_ConvexShapesByMesh.TryGetValue(GetMeshKey(meshid, physicsShape), out shape);

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(ObjectPart.PrimitiveShape primShape, out PhysicsConvexShape shape) =>
            m_ConvexShapesByPrimShape.TryGetValue(primShape, out shape);

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(UUID meshid, PrimitivePhysicsShapeType physicsShape) =>
            m_ConvexShapesByMesh.ContainsKey(GetMeshKey(meshid, physicsShape));

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(ObjectPart.PrimitiveShape primShape) =>
            m_ConvexShapesByPrimShape.ContainsKey(primShape);

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(UUID meshid, PrimitivePhysicsShapeType physicsShape) =>
            m_ConvexShapesByMesh.Remove(GetMeshKey(meshid, physicsShape));

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(ObjectPart.PrimitiveShape primShape) =>
            m_ConvexShapesByPrimShape.Remove(primShape);

        void ISimulationDataPhysicsConvexStorageInterface.RemoveAll()
        {
            m_ConvexShapesByMesh.Clear();
            m_ConvexShapesByPrimShape.Clear();
        }

        void IPhysicsHacdCleanCache.CleanCache()
        {
            ((ISimulationDataPhysicsConvexStorageInterface)this).RemoveAll();
        }

        HacdCleanCacheOrder IPhysicsHacdCleanCache.CleanOrder => HacdCleanCacheOrder.BeforePhysicsShapeManager;
    }
}