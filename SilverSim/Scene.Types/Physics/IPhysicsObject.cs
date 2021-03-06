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

using SilverSim.Types;

namespace SilverSim.Scene.Types.Physics
{
    public interface IPhysicsObject
    {
        /* position, acceleration, velocity (angular and linear) is pushed to target object */
        void SetAppliedForce(Vector3 value);
        void SetAppliedTorque(Vector3 value);
        void SetLinearImpulse(Vector3 value);
        void SetAngularImpulse(Vector3 value);

        double Mass { get; }

        double Buoyancy { get; set; }

        Vector3 Torque { get; }
        Vector3 Force { get; }

        /* Vehicle model is now accessed through shared memory class VehicleParams */

        void ReceiveState(PhysicsStateData data, Vector3 positionOffset);

        void SetHoverHeight(double height, bool water, double tau);
        void StopHover();

        void SetLookAt(Quaternion q, double strength, double damping);
        void StopLookAt();

        bool ActivateTargetList(Vector3[] targetList);
        void DeactivateTargetList();
    }
}
