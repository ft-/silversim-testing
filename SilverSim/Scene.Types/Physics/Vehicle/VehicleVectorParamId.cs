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

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    public enum VehicleVectorParamId
    {
        AngularFrictionTimescale = 17,
        AngularMotorDirection = 19,
        LinearFrictionTimescale = 16,
        LinearMotorDirection = 18,
        LinearMotorOffset = 20,

        /* some of these are not vector based but we make them so */
        LinearDeflectionEfficiency = 28,
        LinearDeflectionTimescale = 29,
        LinearMotorTimescale = 30,
        LinearMotorDecayTimescale = 31,
        AngularDeflectionEfficiency = 32,
        AngularDeflectionTimescale = 33,
        AngularMotorTimescale = 34,
        AngularMotorDecayTimescale = 35,
        VerticalAttractionEfficiency = 36,
        VerticalAttractionTimescale = 37,

        LinearWindEfficiency = 12001,
        AngularWindEfficiency = 12002,

        LinearMotorAccelPosTimescale = 13000,
        LinearMotorDecelPosTimescale = 13001,
        LinearMotorAccelNegTimescale = 13002,
        LinearMotorDecelNegTimescale = 13003,

        AngularMotorAccelPosTimescale = 13100,
        AngularMotorDecelPosTimescale = 13101,
        AngularMotorAccelNegTimescale = 13102,
        AngularMotorDecelNegTimescale = 13103,

        LinearMoveToTargetEfficiency = 14000,
        LinearMoveToTargetTimescale = 14001,
        LinearMoveToTargetEpsilon = 14002,
        LinearMoveToTargetMaxOutput = 14003,

        AngularMoveToTargetEfficiency = 14100,
        AngularMoveToTargetTimescale = 14101,
        AngularMoveToTargetEpsilon = 14102,
        AngularMoveToTargetMaxOutput = 14103
    }
}
