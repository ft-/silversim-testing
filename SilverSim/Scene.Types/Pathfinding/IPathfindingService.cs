﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Pathfinding
{
    public struct WaypointData
    {
        public Vector3 Position;
        public bool IsFlying;
    }

    public enum ResolvePathStatus
    {
        Success = 0,
        GoalReached = 1,
        InvalidStart = 2,
        InvalidGoal = 3,
        Unreachable = 4,
        TargetGone = 5,
        NoValidDestination = 6,
        EvadeHidden = 7,
        EvadeSpotted = 8,
        NoNavMesh = 9,
        DynamicPathfindingDisabled = 10,
        ParcelUnreachable = 11
    }

    public interface IPathfindingService
    {
        ResolvePathStatus TryResolvePath(Vector3 fromPos, Vector3 toPos, CharacterInfo characterInfo, out List<WaypointData> waypoints);
        bool TryGetClosestNavPoint(Vector3 targetPoint, double distanceLimit, bool useStaticOnly, CharacterType type, out Vector3 navPoint);
        void Stop();
        void TriggerRebuild();
        bool IsDynamicEnabled { get; }
    }

    public interface IPathfindingServiceFactory
    {
        IPathfindingService Instantiate(SceneInterface scene);
    }
}
