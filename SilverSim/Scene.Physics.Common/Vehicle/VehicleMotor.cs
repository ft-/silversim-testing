// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;

namespace SilverSim.Scene.Physics.Common.Vehicle
{
    public class VehicleMotor
    {
        readonly VehicleParams m_Params;

        internal VehicleMotor(VehicleParams param)
        {
            m_Params = param;
        }

        public void Process(double dt, PhysicsStateData currentState)
        {

        }

        public Vector3 LinearForce { get; }
        public Vector3 AngularTorque { get; }
    }

    public static class VehicleMotorExtension
    {
        public static VehicleMotor GetMotor(this VehicleParams param)
        {
            return new VehicleMotor(param);
        }
    }
}
