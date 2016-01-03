// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    public enum VehicleFloatParamId
    {
        AngularDeflectionEfficiency = 32,
        AngularDeflectionTimescale = 33,
        BankingEfficiency = 38,
        BankingMix = 39,
        BankingTimescale = 40,
        Buoyancy = 27,
        HoverHeight = 24,
        HoverEfficiency = 25,
        HoverTimescale = 26,

        LinearMotorDecayTimescale = 31,
        LinearMotorTimescale = 30,

        AngularMotorDecayTimescale = 35,
        AngularMotorTimescale = 34,

        MouselookAzimuth = 11001,
        MouselookAltitude = 11002,
        BankingAzimuth = 11003,
        DisableMotorsAbove = 11004,
        DisableMotorsAfter = 11005,
        InvertedBankingModifier = 11006,
    }
}
