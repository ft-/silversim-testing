// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Economy
{
    public class EconomyInfo
    {
        public Int32 ObjectCapacity;
        public Int32 ObjectCount;
        public Int32 PriceEnergyUnit;
        public Int32 PriceObjectClaim;
        public Int32 PricePublicObjectDecay;
        public Int32 PricePublicObjectDelete;
        public Int32 PriceParcelClaim;
        public double PriceParcelClaimFactor = 1;
        public Int32 PriceUpload;
        public Int32 PriceRentLight;
        public Int32 TeleportMinPrice;
        public double TeleportPriceExponent;
        public double EnergyEfficiency;
        public double PriceObjectRent;
        public double PriceObjectScaleFactor = 1;
        public Int32 PriceParcelRent;
        public Int32 PriceGroupCreate;

        public EconomyInfo()
        {

        }
    }
}
