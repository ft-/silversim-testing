// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Physics.Vehicle;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Physics
{
    public interface IPhysicsObject
    {
        /* position, acceleration, velocity (angular and linear) is pushed to target object when IsPhysicsActive equals true */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 DeltaLinearVelocity { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 DeltaAngularVelocity { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 AppliedForce { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 AppliedTorque { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 LinearImpulse { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 AngularImpulse { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 ControlTargetVelocity { set; }

        bool IsPhysicsActive { get; set; } /* disables updates of object */
        bool IsPhantom { get; set; }
        bool IsVolumeDetect { get; set; }
        bool ContributesToCollisionSurfaceAsChild { get; set; } /* set to true when physics object contributes to collision surface in link sets as child prim */
        double Mass { get; }

        double Buoyancy { get; set; }

        VehicleType VehicleType { get; set; }
        VehicleFlags VehicleFlags { get; set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        VehicleFlags SetVehicleFlags { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        VehicleFlags ClearVehicleFlags { set; }
        Quaternion this[VehicleRotationParamId id] { get; set; }
        Vector3 this[VehicleVectorParamId id] { get; set; }
        double this[VehicleFloatParamId id] { get; set; }

        void TransferState(IPhysicsObject target, Vector3 positionOffset);
        void ReceiveState(PhysicsStateData data, Vector3 positionOffset);
    }
}
