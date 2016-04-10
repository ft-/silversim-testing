// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Physics
{
    public interface IPhysicsObject
    {
        /* position, acceleration, velocity (angular and linear) is pushed to target object when IsPhysicsActive equals true */
        void SetDeltaLinearVelocity(Vector3 value);
        void SetDeltaAngularVelocity(Vector3 value);
        void SetAppliedForce(Vector3 value);
        void SetAppliedTorque(Vector3 value);
        void SetLinearImpulse(Vector3 value);
        void SetAngularImpulse(Vector3 value);
        void SetControlTargetVelocity(Vector3 value);

        bool IsPhysicsActive { get; set; } /* disables updates of object */
        bool IsPhantom { get; set; }
        bool IsVolumeDetect { get; set; }
        bool ContributesToCollisionSurfaceAsChild { get; set; } /* set to true when physics object contributes to collision surface in link sets as child prim */
        double Mass { get; }

        double Buoyancy { get; set; }

        /* Vehicle model is now accessed through shared memory class VehicleParams */

        void TransferState(IPhysicsObject target, Vector3 positionOffset);
        void ReceiveState(PhysicsStateData data, Vector3 positionOffset);
    }
}
