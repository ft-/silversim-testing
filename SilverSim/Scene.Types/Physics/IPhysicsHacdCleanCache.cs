// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Scene.Types.Physics
{
    public enum HacdCleanCacheOrder
    {
        BeforePhysicsShapeManager = -1,
        /** <summary>Do not use this one for anything else than PhysicsShapeManager</summary> */
        PhysicsShapeManager = 0,
        AfterPhysicsShapeManager = 1
    }

    public interface IPhysicsHacdCleanCache
    {
        void CleanCache();
        HacdCleanCacheOrder CleanOrder { get; }
    }
}
