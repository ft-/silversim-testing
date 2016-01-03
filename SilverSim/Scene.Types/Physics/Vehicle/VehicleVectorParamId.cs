// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    public enum VehicleVectorParamId
    {
        AngularFrictionTimescale = 17,
        AngularMotorDirection = 19,
        LinearFrictionTimescale = 16,
        LinearMotorDirection = 18,
        LinearMotorOffset = 20,

        /* enable use of these as vector parameters */
        LinearMotorDecayTimescale = 31,
        LinearMotorTimescale = 30,

        AngularMotorDecayTimescale = 35,
        AngularMotorTimescale = 34,

    }
}
