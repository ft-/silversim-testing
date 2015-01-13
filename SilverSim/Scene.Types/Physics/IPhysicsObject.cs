/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
