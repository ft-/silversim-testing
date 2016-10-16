// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Scene.Types.Object;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataPhysicsConvexStorageInterface
    {
        PhysicsConvexShape this[UUID meshid] { get;  set; }
        PhysicsConvexShape this[ObjectPart.PrimitiveShape primShape] { get;  set; }
        bool TryGetValue(UUID meshid, out PhysicsConvexShape shape);
        bool TryGetValue(ObjectPart.PrimitiveShape primShape, out PhysicsConvexShape shape);
        bool ContainsKey(UUID meshid);
        bool ContainsKey(ObjectPart.PrimitiveShape primShape);
        bool Remove(UUID meshid);
        bool Remove(ObjectPart.PrimitiveShape primShape);
        void RemoveAll();
        ICollection<UUID> KnownMeshIds { get; }
    }
}