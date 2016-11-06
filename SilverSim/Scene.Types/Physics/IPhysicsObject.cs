// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Physics
{
    public interface IPhysicsObject
    {
        /* position, acceleration, velocity (angular and linear) is pushed to target object when IsPhysicsActive equals true */
        void SetAppliedForce(Vector3 value);
        void SetAppliedTorque(Vector3 value);
        void SetLinearImpulse(Vector3 value);
        void SetAngularImpulse(Vector3 value);

        bool IsRotateXEnabled { get; set; }
        bool IsRotateYEnabled { get; set; }
        bool IsRotateZEnabled { get; set; }
        bool IsPhysicsActive { get; set; } /* disables updates of object */
        bool IsPhantom { get; set; }
        bool IsVolumeDetect { get; set; }
        double Mass { get; }

        double Buoyancy { get; set; }

        /* Vehicle model is now accessed through shared memory class VehicleParams */

        void TransferState(IPhysicsObject target, Vector3 positionOffset);
        void ReceiveState(PhysicsStateData data, Vector3 positionOffset);
    }
}
