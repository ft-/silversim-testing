// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Physics.Vehicle;

namespace SilverSim.Scene.Types.Physics
{
    public interface IPhysicsObject
    {
        /* position, acceleration, velocity (angular and linear) is pushed to target object when IsPhysicsActive equals true */
        Vector3 DeltaLinearVelocity { set; }
        Vector3 DeltaAngularVelocity { set; }
        Vector3 AppliedForce { set; }
        Vector3 AppliedTorque { set; }
        Vector3 LinearImpulse { set; }
        Vector3 AngularImpulse { set; }

        bool IsPhysicsActive { get; set; } /* disables updates of object */
        bool IsPhantom { get; set; }
        bool IsVolumeDetect { get; set; }
        bool ContributesToCollisionSurfaceAsChild { get; set; } /* set to true when physics object contributes to collision surface in link sets as child prim */
        double Mass { get; }

        double Buoyancy { get; set; }

        VehicleType VehicleType { get; set; }
        VehicleFlags VehicleFlags { get; set; }
        VehicleFlags SetVehicleFlags { set; }
        VehicleFlags ClearVehicleFlags { set; }
        Quaternion this[VehicleRotationParamId id] { get; set; }
        Vector3 this[VehicleVectorParamId id] { get; set; }
        double this[VehicleFloatParamId id] { get; set; }
    }
}
