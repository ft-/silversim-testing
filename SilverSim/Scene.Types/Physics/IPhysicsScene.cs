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

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Physics
{
    public struct RayResult
    {
        public UUID ObjectId;
        public UUID PartId;
        public Vector3 HitNormalWorld;
        public Vector3 HitPointWorld;
        public bool IsTerrain;
    }

    [Flags]
    public enum RayTestHitFlags : ulong
    {
        None = 0,
        Phantom = 1,
        NonPhantom = 2,
        Physical = 4,
        NonPhysical = 8,
        Avatar = 16,
        Character = 32,
        Terrain = 64,
        All = ~0UL
    }

    public interface IPhysicsScene
    {
        void RemoveAll();

        void Shutdown();

        double PhysicsFPS { get; }

        double PhysicsFPSNormalized { get; }

        /** <summary>physics dilation time in seconds</summary> */
        double PhysicsDilationTime { get; }

        /** <summary>physics execution time in seconds</summary> */
        double PhysicsExecutionTime { get; }

        uint PhysicsFrameNumber { get; }

        string PhysicsEngineName { get; }

        /* following two hit everything */
        RayResult[] ClosestRayTest(Vector3 rayFromWorld, Vector3 rayToWorld);
        RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld);

        /* next two hit specific based on flags */
        RayResult[] ClosestRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags);
        RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags);
        RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags, uint maxHits);

        Dictionary<uint, double> GetTopColliders();
    }
}
