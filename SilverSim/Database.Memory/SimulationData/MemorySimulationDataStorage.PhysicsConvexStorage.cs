// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.SimulationData
{
    partial class MemorySimulationDataStorage : ISimulationDataPhysicsConvexStorageInterface
    {
        readonly RwLockedDictionary<UUID, PhysicsConvexShape> m_ConvexShapesByMesh = new RwLockedDictionary<UUID, PhysicsConvexShape>();
        readonly RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape> m_ConvexShapesByPrimShape = new RwLockedDictionary<ObjectPart.PrimitiveShape, PhysicsConvexShape>();

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[UUID meshid]
        {
            get
            {
                return m_ConvexShapesByMesh[meshid];
            }
            set
            {
                m_ConvexShapesByMesh[meshid] = value;
            }
        }

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[ObjectPart.PrimitiveShape primShape]
        {
            get
            {
                return m_ConvexShapesByPrimShape[primShape];
            }
            set
            {
                m_ConvexShapesByPrimShape[primShape] = value;
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(UUID meshid, out PhysicsConvexShape shape)
        {
            return m_ConvexShapesByMesh.TryGetValue(meshid, out shape);
        }

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(ObjectPart.PrimitiveShape primShape, out PhysicsConvexShape shape)
        {
            return m_ConvexShapesByPrimShape.TryGetValue(primShape, out shape);
        }

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(UUID meshid)
        {
            return m_ConvexShapesByMesh.ContainsKey(meshid);
        }

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(ObjectPart.PrimitiveShape primShape)
        {
            return m_ConvexShapesByPrimShape.ContainsKey(primShape);
        }

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(UUID meshid)
        {
            return m_ConvexShapesByMesh.Remove(meshid);
        }

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(ObjectPart.PrimitiveShape primShape)
        {
            return m_ConvexShapesByPrimShape.Remove(primShape);
        }

        ICollection<UUID> ISimulationDataPhysicsConvexStorageInterface.KnownMeshIds
        {
            get
            {
                return m_ConvexShapesByMesh.Keys;
            }
        }

        void ISimulationDataPhysicsConvexStorageInterface.RemoveAll()
        {
            m_ConvexShapesByMesh.Clear();
            m_ConvexShapesByPrimShape.Clear();
        }

    }
}